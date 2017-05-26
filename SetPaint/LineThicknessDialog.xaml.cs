/*
 * FILENAME		: LineThicknessDialog.xaml.cs
 * PROJECT		: WMP - SetPaint Project
 * PROGRAMMERS	: Austin Che, Monira Sultana
 * DATE			: 2016/11/27
 * DESCRIPTION	: This file contains the logic for choosing a line thickness.
 */
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
using System.Windows.Shapes;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SetPaint
{
    /// <summary>
    /// Interaction logic for LineThicknessDialog.xaml
    /// </summary>
    public partial class LineThicknessDialog : Window
    {
        public double LineThickness { get; set; }

        /// <summary>
        /// The constructor for the line thickness dialog box
        /// </summary>
        /// <param name="owner">The main window is the owner.</param>
        /// <param name="prevLineThickness">The previous line thickness.</param>
        public LineThicknessDialog(Window owner, double prevLineThickness = 1.00)
        {
            InitializeComponent();
            this.Title = "Line Thickness";
            //this.WindowStartupLocation = owner;
            this.Owner = owner;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.ResizeMode = ResizeMode.NoResize;

            // By default the line thickness is 1px
            LineThickness = 1.0;
            // Configure the combo box
            cBoxLineThickness.IsReadOnly = true;
            cBoxLineThickness.Items.Add("1px");
            cBoxLineThickness.Items.Add("2px");
            cBoxLineThickness.Items.Add("4px");
            cBoxLineThickness.Items.Add("6px");
            cBoxLineThickness.Items.Add("8px");
            cBoxLineThickness.Items.Add("10px");
            GetCurrentLineThicknessSelection(prevLineThickness);
        }

        /// <summary>
        /// Get the line thickness that the user has chosen from a combo box.
        /// </summary>
        /// <param name="sender">The button that was clicked.</param>
        /// <param name="e">The click event.</param>
        private void bttnOK_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = cBoxLineThickness.SelectedIndex;
            if (selectIndex >= 0)
            {
                Regex rxgr = new Regex(@"(\d+)");
                string parseString = (string)cBoxLineThickness.SelectedItem;

                double lineThickness = 0.0;
                // Get the line thickness from the user's selection
                double.TryParse(rxgr.Match(parseString).Value, out lineThickness);
                LineThickness = lineThickness;
            }

            this.Close();
        }

        /// <summary>
        /// Whenever this dialog box is opened, get whatever line thickness
        /// that user currently has. Example: 4px line thickness. Then automatically
        /// go to the corresponding position in the combo box that matches the line thickness.
        /// </summary>
        /// <param name="currentThickness"></param>
        private void GetCurrentLineThicknessSelection(double currentThickness)
        {
            // Using regex to parse digit from "#px"
            Regex rxgr = new Regex(@"(\d+)");

            int cBoxIndex = 0;
            // The combo box contains string objects
            foreach (string str in cBoxLineThickness.Items)
            {
                string pixelStr = (string)str;

                double parseNum = 0.0;
                double.TryParse(rxgr.Match(pixelStr).Value, out parseNum);
                // Compare the current selection box number to the user's previously
                // selected line thickness
                if (parseNum.Equals(currentThickness))
                {
                    cBoxLineThickness.SelectedIndex = cBoxIndex;
                    break;
                }

                ++cBoxIndex;
            }
        }
    }
}
