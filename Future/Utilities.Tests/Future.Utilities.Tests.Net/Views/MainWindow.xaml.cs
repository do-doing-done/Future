using DevExpress.Mvvm;
using DevExpress.Xpf.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Future.Utilities.Tests.Net
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            BuildTree();
        }

        void BuildTree()
        {
            TreeListNode rootNode = CreateRootNode(new ProjectObject() { Name = "Project: Stanton", Executor = "Nicholas Llams" });
            TreeListNode childNode = CreateChildNode(rootNode, new ProjectObject() { Name = "Information Gathering", Executor = "Ankie Galva" });
            CreateChildNode(childNode, new ProjectObject() { Name = "Design", Executor = "Reardon Felton" });
        }

        TreeListNode CreateRootNode(object dataObject)
        {
            TreeListNode rootNode = new TreeListNode(dataObject);
            treeListView1.Nodes.Add(rootNode);
            return rootNode;
        }

        TreeListNode CreateChildNode(TreeListNode parentNode, object dataObject)
        {
            TreeListNode childNode = new TreeListNode(dataObject);
            parentNode.Nodes.Add(childNode);
            return childNode;
        }
    }
}
