//
// Document.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Components;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Tasks;
using Mono.Addins;
using MonoDevelop.Ide.Extensions;
using System.Linq;
using System.Threading;
using MonoDevelop.Ide.TypeSystem;
using System.Text;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Core.Text;

namespace MonoDevelop.Ide.Gui
{

	public class Document : DocumentContext
	{
		internal object MemoryProbe = Counters.DocumentsInMemory.CreateMemoryProbe ();
		
		IWorkbenchWindow window;
		ParsedDocument parsedDocument;
		FilePath analysisDocumentFileName;
		Microsoft.CodeAnalysis.DocumentId analysisDocument;

		const int ParseDelay = 600;

		public IWorkbenchWindow Window {
			get { return window; }
		}
		
		internal DateTime LastTimeActive {
			get;
			set;
		}

		/// <summary>
		/// Returns the roslyn document for this document. This may return <c>null</c> if it's no compileable document.
		/// Even if it's a C# file.
		/// </summary>
		public override Microsoft.CodeAnalysis.Document AnalysisDocument {
			get {
				if (analysisDocument == null)
					return null;
				return TypeSystemService.GetCodeAnalysisDocument (analysisDocument);
			}
		}
 		
		public override T GetContent<T> ()
		{
			if (window == null)
				return null;
			//check whether the ViewContent can return the type directly
			T ret = Window.ActiveViewContent.GetContent (typeof(T)) as T;
			if (ret != null)
				return ret;
			
			//check the primary viewcontent
			//not sure if this is the right thing to do, but things depend on this behaviour
			if (Window.ViewContent != Window.ActiveViewContent) {
				ret = Window.ViewContent.GetContent (typeof(T)) as T;
				if (ret != null)
					return ret;
			}

			//If we didn't find in ActiveView or ViewContent... Try in SubViews
			foreach (var subView in window.SubViewContents) {
				foreach (var cnt in subView.GetContents<T> ()) {
					return cnt;
				}
			}

			return null;
		}
		
		public override IEnumerable<T> GetContents<T> ()
		{
			foreach (var cnt in window.ViewContent.GetContents<T> ()) {
				yield return cnt;
			}

			foreach (var subView in window.SubViewContents) {
				foreach (var cnt in subView.GetContents<T> ()) {
					yield return cnt;
				}
			}
		}


		static Document ()
		{
			if (IdeApp.Workbench != null) {
				IdeApp.Workbench.ActiveDocumentChanged += delegate {
					// reparse on document switch to update the current file with changes done in other files.
					var doc = IdeApp.Workbench.ActiveDocument;
					if (doc == null || doc.Editor == null)
						return;
					doc.StartReparseThread ();
				};
			}
		}

		public Document (IWorkbenchWindow window)
		{
			Counters.OpenDocuments++;
			LastTimeActive = DateTime.Now;
			this.window = window;
			window.Closed += OnClosed;
			window.ActiveViewContentChanged += OnActiveViewContentChanged;
			if (IdeApp.Workspace != null)
				IdeApp.Workspace.ItemRemovedFromSolution += OnEntryRemoved;
			if (window.ViewContent.Project != null)
				window.ViewContent.Project.Modified += HandleProjectModified;
			window.ViewsChanged += HandleViewsChanged;
			window.ViewContent.ContentNameChanged += delegate {
				analysisDocument = null;
			};
			MonoDevelopWorkspace.LoadingFinished += TypeSystemService_WorkspaceItemLoaded;
		}

		void TypeSystemService_WorkspaceItemLoaded (object sender, EventArgs e)
		{
			if (adhocProject == null)
				analysisDocument = null;
			EnsureAnalysisDocumentIsOpen ();
			if (analysisDocument != null)
				StartReparseThread ();
		}

/*		void UpdateRegisteredDom (object sender, ProjectDomEventArgs e)
		{
			if (dom == null || dom.Project == null)
				return;
			var project = e.ITypeResolveContext != null ? e.ITypeResolveContext.Project : null;
			if (project != null && project.FileName == dom.Project.FileName)
				dom = e.ITypeResolveContext;
		}*/

		public FilePath FileName {
			get {
				if (Window == null || !Window.ViewContent.IsFile)
					return null;
				return Window.ViewContent.IsUntitled ? Window.ViewContent.UntitledName : Window.ViewContent.ContentName;
			}
		}

		public bool IsFile {
			get { return Window.ViewContent.IsFile; }
		}
		
		public bool IsDirty {
			get { return !Window.ViewContent.IsViewOnly && (Window.ViewContent.ContentName == null || Window.ViewContent.IsDirty); }
			set { Window.ViewContent.IsDirty = value; }
		}

		FilePath adHocFile;
		Project adhocProject;

		public override Project Project {
			get { return (Window != null ? Window.ViewContent.Project : null); }
/*			set { 
				Window.ViewContent.Project = value; 
				if (value != null)
					singleFileContext = null;
				// File needs to be in sync with the project, otherwise the parsed document at start may be invalid.
				// better solution: create the document with the project attached.
				StartReparseThread ();
			}*/
		}

		public override bool IsCompileableInProject {
			get {
				var project = Project;
				if (project == null)
					return false;
				var solution = project.ParentSolution;

				if (solution != null && IdeApp.Workspace != null) {
					var config = IdeApp.Workspace.ActiveConfiguration;
					if (config != null) {
						var sc = solution.GetConfiguration (config);
						if (sc != null && !sc.BuildEnabledForItem (project))
							return false;
					}
				}

				var pf = project.GetProjectFile (FileName);
				return pf != null && pf.BuildAction != BuildAction.None;
			}
		}

		public Task<Microsoft.CodeAnalysis.Compilation> GetCompilationAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			var project = TypeSystemService.GetCodeAnalysisProject (Project ?? adhocProject); 
			if (project == null)
				return new Task<Microsoft.CodeAnalysis.Compilation> (() => null);
			return project.GetCompilationAsync (cancellationToken);
		}

		public override ParsedDocument ParsedDocument {
			get {
				return parsedDocument;
			}
		}
		
		public string PathRelativeToProject {
			get { return Window.ViewContent.PathRelativeToProject; }
		}
		
		public void Select ()
		{
			window.SelectWindow ();
		}
		
		public DocumentView ActiveView {
			get {
				LoadViews (true);
				return WrapView (window.ActiveViewContent);
			}
		}
		
		public DocumentView PrimaryView {
			get {
				LoadViews (true);
				return WrapView (window.ViewContent);
			}
		}

		public ReadOnlyCollection<DocumentView> Views {
			get {
				LoadViews (true);
				if (viewsRO == null)
					viewsRO = new ReadOnlyCollection<DocumentView> (views);
				return viewsRO;
			}
		}

		ReadOnlyCollection<DocumentView> viewsRO;
		List<DocumentView> views = new List<DocumentView> ();

		void HandleViewsChanged (object sender, EventArgs e)
		{
			LoadViews (false);
		}

		void LoadViews (bool force)
		{
			if (!force && views == null)
				return;
			var newList = new List<DocumentView> ();
			newList.Add (WrapView (window.ViewContent));
			foreach (var v in window.SubViewContents)
				newList.Add (WrapView (v));
			views = newList;
			viewsRO = null;
		}

		DocumentView WrapView (IBaseViewContent content)
		{
			if (content == null)
				return null;
			if (views != null)
				return views.FirstOrDefault (v => v.BaseContent == content) ?? new DocumentView (this, content);
			else
				return new DocumentView (this, content);
		}

		public override string Name {
			get {
				IViewContent view = Window.ViewContent;
				return view.IsUntitled ? view.UntitledName : view.ContentName;
			}
		}

		public TextEditor Editor {
			get {
				return GetContent <TextEditor> ();
			}
		}

		public bool IsViewOnly {
			get { return Window.ViewContent.IsViewOnly; }
		}
		
		public void Reload ()
		{
			ICustomXmlSerializer memento = null;
			IMementoCapable mc = GetContent<IMementoCapable> ();
			if (mc != null) {
				memento = mc.Memento;
			}
			window.ViewContent.Load (window.ViewContent.ContentName);
			if (memento != null) {
				mc.Memento = memento;
			}
		}
		
		public void Save ()
		{
			// suspend type service "check all file loop" since we have already a parsed document.
			// Or at least one that updates "soon".
			TypeSystemService.TrackFileChanges = false;
			try {
				if (Window.ViewContent.IsViewOnly || !Window.ViewContent.IsDirty)
					return;
	
				if (!Window.ViewContent.IsFile) {
					Window.ViewContent.Save ();
					return;
				}
				
				if (Window.ViewContent.ContentName == null) {
					SaveAs ();
				} else {
					try {
						FileService.RequestFileEdit (Window.ViewContent.ContentName, true);
					} catch (Exception ex) {
						MessageService.ShowError (GettextCatalog.GetString ("The file could not be saved."), ex.Message, ex);
					}
					
					FileAttributes attr = FileAttributes.ReadOnly | FileAttributes.Directory | FileAttributes.Offline | FileAttributes.System;
	
					if (!File.Exists (Window.ViewContent.ContentName) || (File.GetAttributes (window.ViewContent.ContentName) & attr) != 0) {
						SaveAs ();
					} else {
						string fileName = Window.ViewContent.ContentName;
						// save backup first						
						if ((bool)PropertyService.Get ("SharpDevelop.CreateBackupCopy", false)) {
							Window.ViewContent.Save (fileName + "~");
							FileService.NotifyFileChanged (fileName);
						}
						Window.ViewContent.Save (fileName);
						FileService.NotifyFileChanged (fileName);
						OnSaved (EventArgs.Empty);
					}
				}
			} finally {
				// Set the file time of the current document after the file time of the written file, to prevent double file updates.
				// Note that the parsed document may be overwritten by a background thread to a more recent one.
				var doc = parsedDocument;
				if (doc != null) {
					string fileName = Window.ViewContent.ContentName;
					try {
						// filename could be null if the user cancelled SaveAs and this is a new & unsaved file
						if (fileName != null)
							doc.LastWriteTimeUtc = File.GetLastWriteTimeUtc (fileName);
					} catch (Exception e) {
						doc.LastWriteTimeUtc = DateTime.UtcNow;
						LoggingService.LogWarning ("Exception while getting the write time from " + fileName, e); 
					}
				}
				TypeSystemService.TrackFileChanges = true;
			}
		}

		public void SaveAs ()
		{
			SaveAs (null);
		}
		
		public void SaveAs (string filename)
		{
			if (Window.ViewContent.IsViewOnly || !Window.ViewContent.IsFile)
				return;


			Encoding encoding = null;
			
			var tbuffer = GetContent <ITextSource> ();
			if (tbuffer != null) {
				encoding = tbuffer.Encoding;
				if (encoding == null)
					encoding = Encoding.UTF8;
			}
				
			if (filename == null) {
				var dlg = new OpenFileDialog (GettextCatalog.GetString ("Save as..."), FileChooserAction.Save) {
					TransientFor = IdeApp.Workbench.RootWindow,
					Encoding = encoding,
					ShowEncodingSelector = (tbuffer != null),
				};
				if (Window.ViewContent.IsUntitled)
					dlg.InitialFileName = Window.ViewContent.UntitledName;
				else {
					dlg.CurrentFolder = Path.GetDirectoryName (Window.ViewContent.ContentName);
					dlg.InitialFileName = Path.GetFileName (Window.ViewContent.ContentName);
				}
				
				if (!dlg.Run ())
					return;
				
				filename = dlg.SelectedFile;
				encoding = dlg.Encoding;
			}
		
			if (!FileService.IsValidPath (filename)) {
				MessageService.ShowMessage (GettextCatalog.GetString ("File name {0} is invalid", filename));
				return;
			}
			// detect preexisting file
			if (File.Exists (filename)) {
				if (!MessageService.Confirm (GettextCatalog.GetString ("File {0} already exists. Overwrite?", filename), AlertButton.OverwriteFile))
					return;
			}
			
			// save backup first
			if ((bool)PropertyService.Get ("SharpDevelop.CreateBackupCopy", false)) {
				if (tbuffer != null && encoding != null)
					TextFileUtility.WriteText (filename + "~", tbuffer.Text, encoding, tbuffer.UseBOM);
				else
					Window.ViewContent.Save (new FileSaveInformation (filename + "~", encoding));
			}
			TypeSystemService.RemoveSkippedfile (FileName);
			// do actual save
			Window.ViewContent.Save (new FileSaveInformation (filename + "~", encoding));

			FileService.NotifyFileChanged (filename);
			DesktopService.RecentFiles.AddFile (filename, (Project)null);
			
			OnSaved (EventArgs.Empty);
			UpdateParseDocument ();
		}
		
		public bool IsBuildTarget
		{
			get
			{
				if (this.IsViewOnly)
					return false;
				if (Window.ViewContent.ContentName != null)
					return Services.ProjectService.CanCreateSingleFileProject(Window.ViewContent.ContentName);
				
				return false;
			}
		}
		
		public IAsyncOperation Build ()
		{
			return IdeApp.ProjectOperations.BuildFile (Window.ViewContent.ContentName);
		}
		
		public IAsyncOperation Rebuild ()
		{
			return Build ();
		}
		
		public void Clean ()
		{
		}
		
		public IAsyncOperation Run ()
		{
			return Run (Runtime.ProcessService.DefaultExecutionHandler);
		}

		public IAsyncOperation Run (IExecutionHandler handler)
		{
			return IdeApp.ProjectOperations.ExecuteFile (Window.ViewContent.ContentName, handler);
		}

		public bool CanRun ()
		{
			return CanRun (Runtime.ProcessService.DefaultExecutionHandler);
		}
		
		public bool CanRun (IExecutionHandler handler)
		{
			return IsBuildTarget && Window.ViewContent.ContentName != null && IdeApp.ProjectOperations.CanExecuteFile (Window.ViewContent.ContentName, handler);
		}
		
		public bool Close ()
		{
			return ((SdiWorkspaceWindow)Window).CloseWindow (false, true);
		}

		protected override void OnSaved (EventArgs e)
		{
			IdeApp.Workbench.SaveFileStatus ();
			base.OnSaved (e);
		}

		public void CancelParseTimeout ()
		{
			if (parseTimeout != 0) {
				GLib.Source.Remove (parseTimeout);
				parseTimeout = 0;
			}
		}
		
		bool isClosed;
		void OnClosed (object s, EventArgs a)
		{
			isClosed = true;
//			TypeSystemService.DomRegistered -= UpdateRegisteredDom;
			CancelParseTimeout ();
			ClearTasks ();
			TypeSystemService.RemoveSkippedfile (FileName);


			try {
				OnClosed (a);
			} catch (Exception ex) {
				LoggingService.LogError ("Exception while calling OnClosed.", ex);
			}

			// Parse the file when the document is closed. In this way if the document
			// is closed without saving the changes, the saved compilation unit
			// information will be restored
/*			if (currentParseFile != null) {
				TypeSystemService.QueueParseJob (dom, delegate (string name, IProgressMonitor monitor) {
					TypeSystemService.Parse (curentParseProject, currentParseFile);
				}, FileName);
			}
			if (isFileDom) {
				TypeSystemService.RemoveFileDom (FileName);
				dom = null;
			}*/
			
			Counters.OpenDocuments--;
		}

		internal void DisposeDocument ()
		{
			if (analysisDocument != null) {
				TypeSystemService.InformDocumentClose (analysisDocument, FileName);
				analysisDocument = null;
			}
			UnloadAdhocProject ();
			if (Editor != null) {
				Editor.Dispose ();
			}
			if (window is SdiWorkspaceWindow)
				((SdiWorkspaceWindow)window).DetachFromPathedDocument ();
			window.Closed -= OnClosed;
			window.ActiveViewContentChanged -= OnActiveViewContentChanged;
			if (IdeApp.Workspace != null)
				IdeApp.Workspace.ItemRemovedFromSolution -= OnEntryRemoved;

			// Unsubscribe project events
			if (window.ViewContent.Project != null)
				window.ViewContent.Project.Modified -= HandleProjectModified;
			window.ViewsChanged += HandleViewsChanged;
			TypeSystemService.Workspace.WorkspaceChanged -= HandleWorkspaceChanged;
			MonoDevelopWorkspace.LoadingFinished -= TypeSystemService_WorkspaceItemLoaded;

			window = null;

			parsedDocument = null;
			views = null;
			viewsRO = null;
		}
#region document tasks
		object lockObj = new object ();
		
		void ClearTasks ()
		{
			lock (lockObj) {
				TaskService.Errors.ClearByOwner (this);
			}
		}
		
//		void CompilationUnitUpdated (object sender, ParsedDocumentEventArgs args)
//		{
//			if (this.FileName == args.FileName) {
////				if (!args.Unit.HasErrors)
//				parsedDocument = args.ParsedDocument;
///* TODO: Implement better task update algorithm.
//
//				ClearTasks ();
//				lock (lockObj) {
//					foreach (Error error in args.Unit.Errors) {
//						tasks.Add (new Task (this.FileName, error.Message, error.Column, error.Line, error.ErrorType == ErrorType.Error ? TaskType.Error : TaskType.Warning, this.Project));
//					}
//					IdeApp.Services.TaskService.AddRange (tasks);
//				}*/
//			}
//		}
#endregion
		void OnActiveViewContentChanged (object s, EventArgs args)
		{
			OnViewChanged (args);
		}
		
		void OnClosed (EventArgs args)
		{
			if (Closed != null)
				Closed (this, args);
		}
		
		void OnViewChanged (EventArgs args)
		{
			if (ViewChanged != null)
				ViewChanged (this, args);
		}
		
		bool wasEdited;

		void InitializeExtensionChain ()
		{
			Editor.InitializeExtensionChain (this);

			if (window is SdiWorkspaceWindow)
				((SdiWorkspaceWindow)window).AttachToPathedDocument (GetContent<MonoDevelop.Ide.Gui.Content.IPathedDocument> ());

		}

		void InitializeEditor ()
		{
			Editor.TextChanged += (o, a) => {
				if (parsedDocument != null)
					parsedDocument.IsInvalid = true;

				if (Editor.IsInAtomicUndo) {
					wasEdited = true;
				} else {
					StartReparseThread ();
				}
			};
			
			Editor.BeginAtomicUndoOperation += delegate {
				wasEdited = false;
			};
			
			Editor.EndAtomicUndoOperation += delegate {
				if (wasEdited)
					StartReparseThread ();
			};
//			Editor.Undone += (o, a) => StartReparseThread ();
//			Editor.Redone += (o, a) => StartReparseThread ();

			InitializeExtensionChain ();
		}
		
		internal void OnDocumentAttached ()
		{
			if (Editor != null) {
				InitializeEditor ();
				RunWhenLoaded (delegate { ListenToProjectLoad (Project); });
			}
			
			window.Document = this;
		}
		
		/// <summary>
		/// Performs an action when the content is loaded.
		/// </summary>
		/// <param name='action'>
		/// The action to run.
		/// </param>
		public void RunWhenLoaded (System.Action action)
		{
			var e = Editor;
			if (e == null) {
				action ();
				return;
			}
			e.RunWhenLoaded (action);
		}

		public override void AttachToProject (Project project)
		{
			SetProject (project);
		}

		internal void SetProject (Project project)
		{
			if (Window == null || Window.ViewContent == null || Window.ViewContent.Project == project)
				return;
			UnloadAdhocProject ();
			if (adhocProject == null) 
				analysisDocument = null;
			ISupportsProjectReload pr = GetContent<ISupportsProjectReload> ();
			if (pr != null) {
				// Unsubscribe project events
				if (Window.ViewContent.Project != null)
					Window.ViewContent.Project.Modified -= HandleProjectModified;
				Window.ViewContent.Project = project;
				pr.Update (project);
			}
			if (project != null)
				project.Modified += HandleProjectModified;
			InitializeExtensionChain ();
			TypeSystemService.Workspace.WorkspaceChanged += HandleWorkspaceChanged;
			ListenToProjectLoad (project);
		}

		void HandleWorkspaceChanged (object sender, Microsoft.CodeAnalysis.WorkspaceChangeEventArgs e)
		{
			if (e.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.DocumentChanged && e.DocumentId == analysisDocument) {
				OnDocumentParsed (EventArgs.Empty);
			}
		}

		void ListenToProjectLoad (Project project)
		{
			StartReparseThread ();
		}

		void HandleInLoadChanged (object sender, EventArgs e)
		{
			StartReparseThread ();
		}

		void HandleProjectModified (object sender, SolutionItemModifiedEventArgs e)
		{
			if (!e.Any (x => x.Hint == "TargetFramework" || x.Hint == "References"))
				return;
			StartReparseThread ();
		}

		/// <summary>
		/// This method can take some time to finish. It's not threaded
		/// </summary>
		/// <returns>
		/// A <see cref="ParsedDocument"/> that contains the current dom.
		/// </returns>
		public override ParsedDocument UpdateParseDocument ()
		{
			try {
				EnsureAnalysisDocumentIsOpen ();
				string currentParseFile = FileName;
				var editor = Editor;
				if (editor == null || string.IsNullOrEmpty (currentParseFile))
					return null;
				TypeSystemService.AddSkippedFile (currentParseFile);
				var currentParseText = editor.CreateDocumentSnapshot ();
				CancelOldParsing();
				var project = Project ?? adhocProject;
				if (project != null && TypeSystemService.CanParseProjections (project, Editor.MimeType, FileName)) {
					var task = TypeSystemService.ParseProjection (project, currentParseFile, editor.MimeType, currentParseText);
					if (task.Result != null) {
						var p = task.Result;
						this.parsedDocument = p.ParsedDocument;
						var projection = p.Projection;
						projection.CreateProjectedEditor (this);
						Editor.SetOrUpdateProjections (this, new [] { projection }, p.DisabledProjectionFeatures);
					}
				} else { 
					this.parsedDocument = TypeSystemService.ParseFile (project, currentParseFile, editor.MimeType, currentParseText).Result ?? this.parsedDocument;
				}
			} finally {

				OnDocumentParsed (EventArgs.Empty);
			}
			return this.parsedDocument;
		}
			
		uint parseTimeout = 0;

		void EnsureAnalysisDocumentIsOpen ()
		{
			if (analysisDocument != null)
				return;
			if (Editor == null) {
				analysisDocument = null;
				return;
			}
			analysisDocumentFileName = FileName;
			if (Project != null) {
				RoslynWorkspace = TypeSystemService.GetWorkspace (this.Project.ParentSolution);
				analysisDocument = TypeSystemService.GetDocumentId (this.Project, this.FileName);
				if (analysisDocument != null) {
					TypeSystemService.InformDocumentOpen (analysisDocument, Editor);
				}
			} else {
				lock (adhocProjectLock) {
					if (adhocProject != null) {
						return;
					}
					if (Editor != null && Editor.MimeType == "text/x-csharp") {
						var newProject = new DotNetAssemblyProject (Microsoft.CodeAnalysis.LanguageNames.CSharp);
						this.adhocProject = newProject;

						newProject.Name = "InvisibleProject";
						newProject.References.Add (new ProjectReference (ReferenceType.Package, "mscorlib"));
						newProject.References.Add (new ProjectReference (ReferenceType.Package, "System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
						newProject.References.Add (new ProjectReference (ReferenceType.Package, "System.Core"));

						newProject.FileName = "test.csproj";
						adHocFile = Platform.IsWindows ? "C:\\a.cs" : "/a.cs";
						newProject.Files.Add (new ProjectFile (adHocFile, BuildAction.Compile));

						var solution = new Solution ();
						solution.AddConfiguration ("", true);
						solution.DefaultSolutionFolder.AddItem (newProject);
						using (var monitor = new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ())
							RoslynWorkspace = TypeSystemService.Load (solution, monitor, false);
						analysisDocument = TypeSystemService.GetDocumentId (RoslynWorkspace, adhocProject, adHocFile);
						TypeSystemService.InformDocumentOpen (RoslynWorkspace, analysisDocument, Editor);
					}
				}
			}
		}
		object adhocProjectLock = new object();

		void UnloadAdhocProject ()
		{
			lock (adhocProjectLock) {
				if (adhocProject == null)
					return;
				TypeSystemService.Unload (adhocProject.ParentSolution.ParentWorkspace);
				adhocProject = null;
			}
		}

		object reparseLock = new object();
		CancellationTokenSource parseTokenSource = new CancellationTokenSource();
		int reparseQueue;
		void CancelOldParsing()
		{
			parseTokenSource.Cancel ();
			parseTokenSource = new CancellationTokenSource ();
		}

		internal void StartReparseThread ()
		{
			string currentParseFile = adhocProject != null ? adHocFile : FileName;
			if (string.IsNullOrEmpty (currentParseFile))
				return;
			Interlocked.Increment (ref reparseQueue);
			Application.Invoke (delegate {
				if (Interlocked.Decrement (ref reparseQueue) != 0) {
					return;
				}
				// Don't directly parse the document because doing it at every key press is
				// very inefficient. Do it after a small delay instead, so several changes can
				// be parsed at the same time.
				EnsureAnalysisDocumentIsOpen ();
				CancelParseTimeout ();
				var currentParseText = Editor.CreateSnapshot ();
				string mimeType = Editor.MimeType;
				CancelOldParsing ();
				var token = parseTokenSource.Token;
				var project = Project ?? adhocProject;
				ThreadPool.QueueUserWorkItem (delegate {
					TypeSystemService.AddSkippedFile (currentParseFile);
					if (project != null && TypeSystemService.CanParseProjections (project, mimeType, currentParseFile)) {
						TypeSystemService.ParseProjection (project, currentParseFile, mimeType, currentParseText, token).ContinueWith (task => {
							if (token.IsCancellationRequested)
								return;
							Application.Invoke (delegate {
								// this may be called after the document has closed, in that case the OnDocumentParsed event shouldn't be invoked.
								var taskResult = task.Result;
								if (isClosed || taskResult == null || token.IsCancellationRequested)
									return;
								this.parsedDocument = taskResult.ParsedDocument;
								var projection = taskResult.Projection;
								projection.CreateProjectedEditor (this);
								Editor.SetOrUpdateProjections (this, new [] { projection }, taskResult.DisabledProjectionFeatures);
								OnDocumentParsed (EventArgs.Empty);
							});
						}, TaskContinuationOptions.OnlyOnRanToCompletion);
					} else {
						TypeSystemService.ParseFile (project, currentParseFile, mimeType, currentParseText, token).ContinueWith (task => {
							if (token.IsCancellationRequested)
								return;
							Application.Invoke (delegate {
								// this may be called after the document has closed, in that case the OnDocumentParsed event shouldn't be invoked.
								if (isClosed || task.Result == null || token.IsCancellationRequested)
									return;
								this.parsedDocument = task.Result;
								OnDocumentParsed (EventArgs.Empty);
							});
						}, TaskContinuationOptions.OnlyOnRanToCompletion);
					}
					parseTimeout = 0;
				});
			});
		}
		
		/// <summary>
		/// This method kicks off an async document parser and should be used instead of 
		/// <see cref="UpdateParseDocument"/> unless you need the parsed document immediately.
		/// </summary>
		public override void ReparseDocument ()
		{
			StartReparseThread ();
		}
		
		internal object ExtendedCommandTargetChain {
			get {
				// Only go through the text editor chain, if the text editor is selected as subview
				if (Window != null && Window.ActiveViewContent.GetContent (typeof(TextEditor)) != null)
					return Editor.CommandRouter;
				return null;
			}
		}

		void OnEntryRemoved (object sender, SolutionItemEventArgs args)
		{
			if (args.SolutionItem == window.ViewContent.Project)
				window.ViewContent.Project = null;
		}
		
		public event EventHandler Closed;
		public event EventHandler ViewChanged;
		

		public string[] CommentTags {
			get {
				if (IsFile)
					return GetCommentTags (FileName);
				else
					return null;
			}
		}
		
		public static string[] GetCommentTags (string fileName)
		{
			//Document doc = IdeApp.Workbench.ActiveDocument;
			string loadedMimeType = DesktopService.GetMimeTypeForUri (fileName);

			var result = TextEditorFactory.GetSyntaxProperties (loadedMimeType, "LineComment");
			if (result != null)
				return result;

			var start = TextEditorFactory.GetSyntaxProperties (loadedMimeType, "BlockCommentStart");
			var end = TextEditorFactory.GetSyntaxProperties (loadedMimeType, "BlockCommentEnd");
			if (start != null && end != null)
				return new [] { start[0], end[0] };
			return null;
		}
	
//		public MonoDevelop.Projects.CodeGeneration.CodeGenerator CreateCodeGenerator ()
//		{
//			return MonoDevelop.Projects.CodeGeneration.CodeGenerator.CreateGenerator (Editor.Document.MimeType, 
//				Editor.Options.TabsToSpaces, Editor.Options.TabSize, Editor.EolMarker);
//		}

		/// <summary>
		/// If the document shouldn't restore the settings after the load it can be disabled with this method.
		/// That is useful when opening a document and programmatically scrolling to a specified location.
		/// </summary>
		public void DisableAutoScroll ()
		{
			if (IsFile)
				FileSettingsStore.Remove (FileName);
		}

		public override OptionSet GetOptionSet ()
		{
			return TypeSystemService.Workspace.Options;
		}
	}
	
	
	[Serializable]
	public sealed class DocumentEventArgs : EventArgs
	{
		public Document Document {
			get;
			set;
		}
		public DocumentEventArgs (Document document)
		{
			this.Document = document;
		}
	}
}

