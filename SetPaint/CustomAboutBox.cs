/*
 * FILENAME		: SetPaintWindow.xaml
 * PROJECT		: WMP - SetPaint Project
 * PROGRAMMERS	: Austin Che, Monira Sultana
 * DATE			: 2016/11/27
 * DESCRIPTION	: This file contains the logic for an about box.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Interop;

namespace SetPaint
{
    public partial class CustomAboutBox : Form
    {
        #region CONSTANTS
        private const string kWindowTitle = "About SetPaint";
        #endregion

        public CustomAboutBox()
        {
            InitializeComponent();

            this.Text = kWindowTitle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.CenterToParent();

            lblVersion.Text = "Version 1.0.0.0\nCopyright: All Copy Rights Reserved \nCompany Name: SET Program ";
            txtboxDescription.ReadOnly = true;

            txtboxDescription.Text = "Welcome to SetPaint! This is a drawing application that allows you to "
                + "draw shapes and lines. These shapes include rectangles and ellipses. You can also choose "
                + "what colors the shapes will be. Have fun drawing!";
        }

        /// <summary>
        /// Closes the about box.
        /// </summary>
        /// <param name="sender">The button that was clicked.</param>
        /// <param name="e">The click event arguments.</param>
        private void bttnOK_Click(object sender, EventArgs e)
        {
            // If the OK button is clicked, close the About Box
            this.Close();
        }
    }
}
