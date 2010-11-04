
// This file has been generated by the GUI designer. Do not modify.
namespace CBinding
{
	public partial class EditPackagesDialog
	{
		private global::Gtk.VPaned vpaned1;
		private global::Gtk.HBox hbox1;
		private global::Gtk.Notebook notebook1;
		private global::Gtk.ScrolledWindow scrolledwindow1;
		private global::Gtk.TreeView normalPackageTreeView;
		private global::Gtk.Label labelSystemPackages;
		private global::Gtk.ScrolledWindow scrolledwindow3;
		private global::Gtk.TreeView projectPackageTreeView;
		private global::Gtk.Label labelProjectPackages;
		private global::Gtk.VBox vbox3;
		private global::Gtk.Fixed fixed1;
		private global::Gtk.Button detailsButton;
		private global::Gtk.Table table1;
		private global::Gtk.Label label3;
		private global::Gtk.ScrolledWindow scrolledwindow2;
		private global::Gtk.TreeView selectedPackageTreeView;
		private global::Gtk.VBox vbox2;
		private global::Gtk.Button removeButton;
		private global::Gtk.Button buttonCancel;
		private global::Gtk.Button buttonOk;

		protected virtual void Build ()
		{
			global::Stetic.Gui.Initialize (this);
			// Widget CBinding.EditPackagesDialog
			this.Name = "CBinding.EditPackagesDialog";
			this.Title = global::Mono.Unix.Catalog.GetString ("Edit packages");
			this.Modal = true;
			// Internal child CBinding.EditPackagesDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vpaned1 = new global::Gtk.VPaned ();
			this.vpaned1.CanFocus = true;
			this.vpaned1.Name = "vpaned1";
			this.vpaned1.Position = 183;
			this.vpaned1.BorderWidth = ((uint)(6));
			// Container child vpaned1.Gtk.Paned+PanedChild
			this.hbox1 = new global::Gtk.HBox ();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.notebook1 = new global::Gtk.Notebook ();
			this.notebook1.CanFocus = true;
			this.notebook1.Name = "notebook1";
			this.notebook1.CurrentPage = 0;
			// Container child notebook1.Gtk.Notebook+NotebookChild
			this.scrolledwindow1 = new global::Gtk.ScrolledWindow ();
			this.scrolledwindow1.CanFocus = true;
			this.scrolledwindow1.Name = "scrolledwindow1";
			this.scrolledwindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow1.Gtk.Container+ContainerChild
			this.normalPackageTreeView = new global::Gtk.TreeView ();
			this.normalPackageTreeView.CanFocus = true;
			this.normalPackageTreeView.Name = "normalPackageTreeView";
			this.scrolledwindow1.Add (this.normalPackageTreeView);
			this.notebook1.Add (this.scrolledwindow1);
			// Notebook tab
			this.labelSystemPackages = new global::Gtk.Label ();
			this.labelSystemPackages.Name = "labelSystemPackages";
			this.labelSystemPackages.LabelProp = global::Mono.Unix.Catalog.GetString ("System Packages");
			this.notebook1.SetTabLabel (this.scrolledwindow1, this.labelSystemPackages);
			this.labelSystemPackages.ShowAll ();
			// Container child notebook1.Gtk.Notebook+NotebookChild
			this.scrolledwindow3 = new global::Gtk.ScrolledWindow ();
			this.scrolledwindow3.CanFocus = true;
			this.scrolledwindow3.Name = "scrolledwindow3";
			this.scrolledwindow3.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow3.Gtk.Container+ContainerChild
			this.projectPackageTreeView = new global::Gtk.TreeView ();
			this.projectPackageTreeView.CanFocus = true;
			this.projectPackageTreeView.Name = "projectPackageTreeView";
			this.scrolledwindow3.Add (this.projectPackageTreeView);
			this.notebook1.Add (this.scrolledwindow3);
			global::Gtk.Notebook.NotebookChild w5 = ((global::Gtk.Notebook.NotebookChild)(this.notebook1 [this.scrolledwindow3]));
			w5.Position = 1;
			// Notebook tab
			this.labelProjectPackages = new global::Gtk.Label ();
			this.labelProjectPackages.Name = "labelProjectPackages";
			this.labelProjectPackages.LabelProp = global::Mono.Unix.Catalog.GetString ("Project Packages");
			this.notebook1.SetTabLabel (this.scrolledwindow3, this.labelProjectPackages);
			this.labelProjectPackages.ShowAll ();
			this.hbox1.Add (this.notebook1);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.hbox1 [this.notebook1]));
			w6.Position = 0;
			// Container child hbox1.Gtk.Box+BoxChild
			this.vbox3 = new global::Gtk.VBox ();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 6;
			// Container child vbox3.Gtk.Box+BoxChild
			this.fixed1 = new global::Gtk.Fixed ();
			this.fixed1.HeightRequest = 21;
			this.fixed1.Name = "fixed1";
			this.fixed1.HasWindow = false;
			this.vbox3.Add (this.fixed1);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox3 [this.fixed1]));
			w7.Position = 0;
			w7.Expand = false;
			w7.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.detailsButton = new global::Gtk.Button ();
			this.detailsButton.WidthRequest = 33;
			this.detailsButton.HeightRequest = 33;
			this.detailsButton.Sensitive = false;
			this.detailsButton.CanFocus = true;
			this.detailsButton.Name = "detailsButton";
			this.detailsButton.UseUnderline = true;
			this.detailsButton.Label = global::Mono.Unix.Catalog.GetString ("...");
			this.vbox3.Add (this.detailsButton);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox3 [this.detailsButton]));
			w8.Position = 1;
			w8.Expand = false;
			w8.Fill = false;
			this.hbox1.Add (this.vbox3);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.hbox1 [this.vbox3]));
			w9.Position = 1;
			w9.Expand = false;
			w9.Fill = false;
			this.vpaned1.Add (this.hbox1);
			global::Gtk.Paned.PanedChild w10 = ((global::Gtk.Paned.PanedChild)(this.vpaned1 [this.hbox1]));
			w10.Resize = false;
			// Container child vpaned1.Gtk.Paned+PanedChild
			this.table1 = new global::Gtk.Table (((uint)(2)), ((uint)(2)), false);
			this.table1.Name = "table1";
			this.table1.RowSpacing = ((uint)(6));
			this.table1.ColumnSpacing = ((uint)(6));
			// Container child table1.Gtk.Table+TableChild
			this.label3 = new global::Gtk.Label ();
			this.label3.Name = "label3";
			this.label3.Xalign = 0F;
			this.label3.Yalign = 0F;
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString ("Selected packages:");
			this.table1.Add (this.label3);
			global::Gtk.Table.TableChild w11 = ((global::Gtk.Table.TableChild)(this.table1 [this.label3]));
			w11.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.scrolledwindow2 = new global::Gtk.ScrolledWindow ();
			this.scrolledwindow2.CanFocus = true;
			this.scrolledwindow2.Name = "scrolledwindow2";
			this.scrolledwindow2.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow2.Gtk.Container+ContainerChild
			this.selectedPackageTreeView = new global::Gtk.TreeView ();
			this.selectedPackageTreeView.CanFocus = true;
			this.selectedPackageTreeView.Name = "selectedPackageTreeView";
			this.scrolledwindow2.Add (this.selectedPackageTreeView);
			this.table1.Add (this.scrolledwindow2);
			global::Gtk.Table.TableChild w13 = ((global::Gtk.Table.TableChild)(this.table1 [this.scrolledwindow2]));
			w13.TopAttach = ((uint)(1));
			w13.BottomAttach = ((uint)(2));
			w13.XOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.vbox2 = new global::Gtk.VBox ();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.removeButton = new global::Gtk.Button ();
			this.removeButton.TooltipMarkup = "Remove selected package from the project.";
			this.removeButton.WidthRequest = 33;
			this.removeButton.HeightRequest = 33;
			this.removeButton.Sensitive = false;
			this.removeButton.CanFocus = true;
			this.removeButton.Name = "removeButton";
			this.removeButton.UseUnderline = true;
			// Container child removeButton.Gtk.Container+ContainerChild
			global::Gtk.Alignment w14 = new global::Gtk.Alignment (0.5F, 0.5F, 0F, 0F);
			// Container child GtkAlignment.Gtk.Container+ContainerChild
			global::Gtk.HBox w15 = new global::Gtk.HBox ();
			w15.Spacing = 2;
			// Container child GtkHBox.Gtk.Container+ContainerChild
			global::Gtk.Image w16 = new global::Gtk.Image ();
			w16.Pixbuf = global::Stetic.IconLoader.LoadIcon (this, "gtk-remove", global::Gtk.IconSize.Menu);
			w15.Add (w16);
			// Container child GtkHBox.Gtk.Container+ContainerChild
			global::Gtk.Label w18 = new global::Gtk.Label ();
			w15.Add (w18);
			w14.Add (w15);
			this.removeButton.Add (w14);
			this.vbox2.Add (this.removeButton);
			global::Gtk.Box.BoxChild w22 = ((global::Gtk.Box.BoxChild)(this.vbox2 [this.removeButton]));
			w22.Position = 0;
			w22.Expand = false;
			w22.Fill = false;
			this.table1.Add (this.vbox2);
			global::Gtk.Table.TableChild w23 = ((global::Gtk.Table.TableChild)(this.table1 [this.vbox2]));
			w23.TopAttach = ((uint)(1));
			w23.BottomAttach = ((uint)(2));
			w23.LeftAttach = ((uint)(1));
			w23.RightAttach = ((uint)(2));
			w23.XOptions = ((global::Gtk.AttachOptions)(4));
			this.vpaned1.Add (this.table1);
			w1.Add (this.vpaned1);
			global::Gtk.Box.BoxChild w25 = ((global::Gtk.Box.BoxChild)(w1 [this.vpaned1]));
			w25.Position = 0;
			w25.Padding = ((uint)(3));
			// Internal child CBinding.EditPackagesDialog.ActionArea
			global::Gtk.HButtonBox w26 = this.ActionArea;
			w26.Name = "dialog1_ActionArea";
			w26.Spacing = 6;
			w26.BorderWidth = ((uint)(5));
			w26.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonCancel = new global::Gtk.Button ();
			this.buttonCancel.CanDefault = true;
			this.buttonCancel.CanFocus = true;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseStock = true;
			this.buttonCancel.UseUnderline = true;
			this.buttonCancel.Label = "gtk-cancel";
			this.AddActionWidget (this.buttonCancel, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w27 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w26 [this.buttonCancel]));
			w27.Expand = false;
			w27.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button ();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			this.AddActionWidget (this.buttonOk, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w28 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w26 [this.buttonOk]));
			w28.Position = 1;
			w28.Expand = false;
			w28.Fill = false;
			if ((this.Child != null)) {
				this.Child.ShowAll ();
			}
			this.DefaultWidth = 580;
			this.DefaultHeight = 449;
			this.Show ();
			this.notebook1.SwitchPage += new global::Gtk.SwitchPageHandler (this.OnNotebook1SwitchPage);
			this.normalPackageTreeView.CursorChanged += new global::System.EventHandler (this.OnNonSelectedPackageCursorChanged);
			this.projectPackageTreeView.CursorChanged += new global::System.EventHandler (this.OnNonSelectedPackageCursorChanged);
			this.detailsButton.Clicked += new global::System.EventHandler (this.OnDetailsButtonClicked);
			this.removeButton.Clicked += new global::System.EventHandler (this.OnRemoveButtonClicked);
			this.removeButton.Clicked += new global::System.EventHandler (this.OnRemoveButtonClick);
			this.selectedPackageTreeView.CursorChanged += new global::System.EventHandler (this.OnSelectedPackagesTreeViewCursorChanged);
			this.buttonCancel.Clicked += new global::System.EventHandler (this.OnCancelButtonClick);
			this.buttonOk.Clicked += new global::System.EventHandler (this.OnOkButtonClick);
		}
	}
}
