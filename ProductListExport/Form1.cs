using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Xml;
using System.IO;
using System.IO.Compression;

using Microsoft.WindowsAPICodePack.Dialogs;

using Excel = Microsoft.Office.Interop.Excel;
using System.Reflection;

namespace ProductListExport
{
    public partial class Form1 : Form
    {
        Excel.Application oXL;
        Excel._Workbook oWB;
        Excel._Worksheet oSheet;
        Excel.Range oRng;

        int currentRow = 1;

        public Form1()
        {
            InitializeComponent();
        }

        public void InitExcel()
        {

            oXL = new Excel.Application();
            oXL.Visible = true;

            // Create a new workbook.
            oWB = (Excel._Workbook)(oXL.Workbooks.Add(Missing.Value));
            oSheet = (Excel._Worksheet)oWB.ActiveSheet;

            // Add column headers
            oSheet.Cells[1, 1] = "Part No.";
            oSheet.Cells[1, 2] = "Material";
            oSheet.Cells[1, 3] = "Thickness";
            oSheet.Cells[1, 4] = "Length";
            oSheet.Cells[1, 5] = "Width";

            // Notes
            oSheet.Cells[1, 6] = "Note 0";
            oSheet.Cells[1, 7] = "Note 1";
            oSheet.Cells[1, 8] = "Note 2";
            oSheet.Cells[1, 9] = "Note 3";

            // Tools
            oSheet.Cells[1, 10] = "Z-Tool File";
            oSheet.Cells[1, 11] = "A-Tool File";
            
            // Set column number formats
            oSheet.Columns[1].NumberFormat = "@";
            oSheet.Columns[2].NumberFormat = "@";
            oSheet.Columns[3].NumberFormat = "#,##0.000";
            oSheet.Columns[4].NumberFormat = "#,##0.0";
            oSheet.Columns[5].NumberFormat = "#,##0.0";

            // Notes
            oSheet.Columns[6].NumberFormat = "@";
            oSheet.Columns[7].NumberFormat = "@";
            oSheet.Columns[8].NumberFormat = "@";
            oSheet.Columns[9].NumberFormat = "@";


            oSheet.Columns.AutoFit();

            currentRow = 2;
        }

        public void WriteExcell(ProductInfo product)
        {
            oSheet.Cells[currentRow, 1] = product.Name;
            oSheet.Cells[currentRow, 2] = product.MaterialType;
            oSheet.Cells[currentRow, 3] = product.MaterialThickness;
            oSheet.Cells[currentRow, 4] = product.FlatLength;
            oSheet.Cells[currentRow, 5] = product.FlatWidth;

            oSheet.Cells[currentRow, 6] = product.getNote(0);
            oSheet.Cells[currentRow, 7] = product.getNote(1);
            oSheet.Cells[currentRow, 8] = product.getNote(2);
            oSheet.Cells[currentRow, 9] = product.getNote(3);

            oSheet.Cells[currentRow, 10] = product.Tools.ToolName("Z");
            oSheet.Cells[currentRow, 11] = product.Tools.ToolName("A");

            currentRow++;
        }


        IEnumerable<TreeNode> Collect(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                yield return node;

                foreach (var child in Collect(node.Nodes))
                    yield return child;
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            InitExcel();

            foreach (var node in Collect(treeView1.Nodes))
            {
                string zipPath = node.Name;
                if (File.Exists(zipPath))
                {
                    ProductInfo product = new ProductInfo(zipPath);
                    WriteExcell(product);
                }

                oSheet.Columns.AutoFit();
            }
        }

        public float StrToInch(string value)
        {
            return float.Parse(value) / 25.4f;
        }

        private void btnAddProductFiles_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = Properties.Settings.Default.lastFolderPath;
            dialog.IsFolderPicker = false;
            dialog.Multiselect = true;
            dialog.Title = "Choose Product Files";

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                foreach (string file in dialog.FileNames)
                {
                    RecursiveAddToTree(file);
                }

                Properties.Settings.Default["lastFolderPath"] = Path.GetDirectoryName(dialog.FileNames.First());
            }
        }

        private void btnAddProductFolders_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = Properties.Settings.Default.lastFolderPath;
            dialog.IsFolderPicker = true;
            dialog.RestoreDirectory = false;
            dialog.Multiselect = true;
            dialog.Title = "Choose Product Folders";

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                foreach (string prodFolder in dialog.FileNames)
                {
                    var node = new TreeNode(getProductOrFileName(prodFolder));
                    treeViewAddNode(node, null);
                    RecursiveAddToTree(prodFolder, node);
                }

                Properties.Settings.Default["lastFolderPath"] = Path.GetDirectoryName(dialog.FileNames.First());
            }
        }

        private void RecursiveAddToTree(string path, TreeNode parent = null)
        {
            FileAttributes attr = File.GetAttributes(path);
            if (!attr.HasFlag(FileAttributes.Directory)) {
                var node = new TreeNode(getProductOrFileName(path));
                treeViewAddNode(node, parent);
                return;
            }

            var directories = Directory.GetDirectories(path);
            var files = Directory.GetFiles(path, "*.zip");

            foreach (var directory in directories)
            {
                var node = new TreeNode(getProductOrFileName(directory));
                treeViewAddNode(node, parent);
                RecursiveAddToTree(directory, node);
            }

            foreach (var file in files)
            {
                var node = new TreeNode(getProductOrFileName(file));
                node.Name = Path.GetFullPath(file);
                treeViewAddNode(node, parent);
            }
        }

        private string getProductOrFileName(string path)
        {
            FileAttributes attr = File.GetAttributes(path);

            if (attr.HasFlag(FileAttributes.Directory))
            {
                return Path.GetFileName(path);
            }
            else
            {
                return Path.GetFileNameWithoutExtension(path);
            }
        }
         
        private void treeViewAddNode(TreeNode node, TreeNode parent)
        {
            if (parent is null)
            {
                treeView1.Nodes.Add(node);
            }
            else
            {
                parent.Nodes.Add(node);
            }
        }

        private void btnClearSelection_Click(object sender, EventArgs e)
        {
            treeView1.Nodes.Clear();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string filePath = e.Node.Name;
            if (File.Exists(filePath))
            {
                ProductInfo product = new ProductInfo(filePath);
                lblProdName.Text = product.Name;
                lblInfoText.Text = product.InfoText;

                lblMaterialTyp.Text =product.MaterialType;
                lblMaterialThk.Text = String.Format("{0:f3}\"", product.MaterialThickness);
                lblFlatLength.Text = String.Format("{0:f2}\"", product.FlatLength);
                lblFlatWidth.Text = String.Format("{0:f2}\"", product.FlatWidth);

                lblNote1.Text = product.getNote(0);
                lblNote2.Text = product.getNote(1);
                lblNote3.Text = product.getNote(2);
                lblNote4.Text = product.getNote(3);

                lblToolZ.Text = product.Tools.ToolName("Z");
                lblToolA.Text = product.Tools.ToolName("A");
                lblToolB.Text = product.Tools.ToolName("B");
            }
            else
            {
                lblProdName.Text = "--";
                lblInfoText.Text = "--";
                lblMaterialTyp.Text = "--";
                lblMaterialThk.Text = "--";
                lblFlatLength.Text = "--";
                lblFlatWidth.Text = "--";

                lblNote1.Text = "--";
                lblNote2.Text = "--";
                lblNote3.Text = "--";
                lblNote4.Text = "--";

                lblToolZ.Text = "--";
                lblToolA.Text = "--";
                lblToolB.Text = "--";
            }
        }

        private void btnDeleteItm_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView1.SelectedNode;
            if (node != null)
            {
                treeView1.SelectedNode.Remove();
            }     
        }

        private void treview1_KeyDeletDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                TreeNode node = treeView1.SelectedNode;
                if (node != null)
                {
                    treeView1.SelectedNode.Remove(); ;
                }
            }
        }
    }
}

public class ProductInfo
{
    XmlDocument doc = new XmlDocument();

    private string name;
    private Tools tools;

    public ProductInfo(string zipPath)
    {
  
        using (ZipArchive archive = ZipFile.OpenRead(zipPath))
        {
            ZipArchiveEntry entry = archive.GetEntry("Product.xml");

            using (var stream = entry.Open())
            using (var reader = new StreamReader(stream))
            {
                doc.LoadXml(reader.ReadToEnd());
            }
        }

        this.name = Path.GetFileNameWithoutExtension(zipPath);
        this.tools = new Tools(TryGetNodeValue("/POS3000Data/Product/@PreferredTools"));
    }

    public string Name
    {
        get { return this.name; }
    } 

    public string MaterialType
    {
        get
        {
            // Material(Stahl,)
            string matId = TryGetNodeValue("/POS3000Data/Product/@PhysicalMaterialId");

            matId = matId.Substring(matId.IndexOf(@"(") + 1);
            matId = matId.TrimEnd(',', ')', ' ');
            return matId.Replace("Stahl", "Steel");
        }
    }
    public float MaterialThickness
    {
        get { return StrToInch(TryGetNodeValue("/POS3000Data/Product/@MaterialThickness")); }
    }
    public float PosFlatWidth
    {
        get { return StrToInch(TryGetNodeValue("/POS3000Data/Product/@FlatWidth")); }
    }

    public float PosFlatLength
    {
        get { return StrToInch(TryGetNodeValue("/POS3000Data/Product/@FlatLength")); }
    }

    public float FlatWidth
    {
        // FlatWidth should be the shorter of flat side lengths
        get { return Math.Min(PosFlatWidth, PosFlatLength) ; }
    }

    public float FlatLength
    {
        // FlatLength should be the longer of flat side lengths
        get { return Math.Max(PosFlatWidth, PosFlatLength); }
    }

    public Tools Tools
    {
        get { return this.tools; }
    }

    public string InfoText
    {
        get { return TryGetNodeValue("/POS3000Data/Product/@InfoText"); }
    }

    public string getNote(int number)
    {
        return TryGetNodeValue(String.Format("/POS3000Data/Product/@AdditionalInfo{0:D2}", number));
    }

    public Image Thumbnail
    {
        get { return null; } //doc.SelectSingleNode("/POS3000Data/Product/@ThumbnailGraphicsPath").Value; }
    }


    // helper methods
    float StrToInch(string value)
    {
        return float.Parse(value) / 25.4f;
    }

    string TryGetNodeValue(string nodePath)
    {
        var node = doc.SelectSingleNode(nodePath);
        if (node != null)
        {
            return node.Value;
        }
        return "--";
    }
}

public class Tools
{
    Dictionary<string, string> toolList = new Dictionary<string, string>();

    public string ToolName(string Axis)
    {
        try
        {
            return toolList[Axis];
        }
        catch
        {
            return null;
        }
    }

    public Tools(string tools)
    {
        // "Z='ToolZ_H170A30R010FR45FB85_35006476.xml';A='ToolA_H098B40_36801276.xml';B='ToolB_H55A30R010_36202902.xml';X='ToolX_PB.xml';"

        foreach ( string tool in tools.Split(';'))
        {
            if (tool is "") continue;

            string[] items = tool.Split('=');
            toolList.Add(items[0], items[1].Trim('\''));
        }
    }
}
