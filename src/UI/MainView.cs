using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;
using System.Data;
using System.Drawing;
using System.Drawing.Design;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

#if WITH_MONO_DESIGN
using Mono.Design;
using DocumentDesigner = Mono.Design.DocumentDesigner;
#endif

namespace mwf_designer
{
	public partial class MainView : Form
	{
		Workspace _workspace;

		public MainView ()
		{
			InitializeComponent ();
			toolbox.ToolPicked += OnToolbox_ToolPicked;
			LoadWorkspace ();
		}

		private IComponent GetPrimarySelection (Document document)
		{
			if (document == null)
				throw new ArgumentNullException ("document");

			ISelectionService service = document.DesignSurface.GetService (typeof (ISelectionService)) as ISelectionService;
			if (service != null)
				return (IComponent) service.PrimarySelection;
			else
				return null;
		}

		private void openToolStripMenuItem_Click (object sender, EventArgs e)
		{			
			OpenFileDialog dialog = new OpenFileDialog ();
			dialog.CheckFileExists = true;
			dialog.Multiselect = false;
			dialog.Filter = "C# Source Code (*.cs)|*.cs|VB.NET Source Code (*.vb)|*.vb";
			if (dialog.ShowDialog () == DialogResult.OK) {
				if (surfaceTabs.TabPages.ContainsKey (dialog.FileName)) {// tab page for file already existing
					surfaceTabs.SelectedTab = surfaceTabs.TabPages[dialog.FileName];
				} else {
					if (CodeProvider.IsValid (dialog.FileName))
						LoadDocument (dialog.FileName, _workspace);
					else
						MessageBox.Show ("No corresponding .Designer file found for " + dialog.FileName);
				}
			}
		}

		private void OnToolbox_ToolPicked (object sender, EventArgs args)
		{
			if (_workspace != null && _workspace.ActiveDocument != null) {
				IDesignerHost host = _workspace.ActiveDocument.DesignSurface.GetService (typeof (IDesignerHost)) as IDesignerHost;
				IToolboxService tb = _workspace.ActiveDocument.DesignSurface.GetService (typeof (IToolboxService)) as IToolboxService;
				if (host != null && tb != null)
					((IToolboxUser)(DocumentDesigner)host.GetDesigner (host.RootComponent)).ToolPicked (tb.GetSelectedToolboxItem ());
			}
		}

		private void LoadWorkspace ()
		{
			_workspace = new Workspace ();
			_workspace.ActiveDocumentChanged += delegate (object sender, ActiveDocumentChangedEventArgs args) {
				propertyGrid.ActiveComponents = new object[] { GetPrimarySelection (args.NewDocument) };

			};
			_workspace.References.ReferencesChanged += delegate {
				PopulateToolbox (toolbox, _workspace.References);
			};
			_workspace.Services.AddService (typeof (IToolboxService), (IToolboxService) toolbox);
			_workspace.Load ();
		}

		// Currently populates with all MWF Controls that have a public ctor with no params
		//
		private void PopulateToolbox (Toolbox toolbox, References references)
		{
			toolbox.Clear ();
			foreach (ToolboxItem item in _workspace.GetToolboxItems ())
				toolbox.AddToolboxItem (item);
		}

		private void LoadDocument (string file, Workspace workspace)
		{
			Document doc = new Document (file, workspace);
			doc.Load ();
			doc.Modified += delegate {
				if (!surfaceTabs.TabPages[file].Text.EndsWith (" *"))
					surfaceTabs.TabPages[file].Text += " *";
			};
			workspace.AddDocument (doc);
			workspace.ActiveDocument = doc;
			surfaceTabs.TabPages.Add (file, Path.GetFileNameWithoutExtension (file));
			surfaceTabs.TabPages[file].Controls.Add ((Control)doc.DesignSurface.View);
			surfaceTabs.SelectedTab = surfaceTabs.TabPages[file];
		}

		
		private void newToolStripMenuItem_Click (object sender, EventArgs e)
		{
			NewFileDialog dialog = new NewFileDialog (TemplateManager.AvailableTemplates);
			if (dialog.ShowDialog () == DialogResult.OK) {
				TemplateManager.WriteCode (dialog.Template, dialog.File, CodeProvider.GetCodeBehindFileName (dialog.File), 
										   dialog.Namespace, dialog.Class);
				this.LoadDocument (dialog.File, _workspace);
			}
		}

		private void exitToolStripMenuItem_Click (object sender, EventArgs e)
		{
			this.Close ();
		}

		private void saveToolStripMenuItem_Click (object sender, EventArgs e)
		{
			if (_workspace.ActiveDocument != null && _workspace.ActiveDocument.IsModified) {
				_workspace.ActiveDocument.Save ();
				surfaceTabs.SelectedTab.Text = Path.GetFileNameWithoutExtension (_workspace.ActiveDocument.FileName);
			}
		}

		private void closeToolStripMenuItem_Click (object sender, EventArgs e)
		{
			if (_workspace.ActiveDocument != null) {
				if (_workspace.ActiveDocument.IsModified)
					_workspace.ActiveDocument.Save ();
				_workspace.ActiveDocument.Dispose ();
				surfaceTabs.TabPages.Remove (surfaceTabs.SelectedTab);
			}
		}

		private void referencesToolStripMenuItem_Click (object sender, EventArgs e)
		{
			new ReferencesDialog (_workspace.References).ShowDialog ();
		}
	}
}