/*
 * FILENAME		: SetPaintWindow.xaml
 * PROJECT		: WMP - SetPaint Project
 * PROGRAMMERS	: Austin Che, Monira Sultana
 * DATE			: 2016/11/27
 * DESCRIPTION	: This file contains the main logic (event handling) for SetPaint.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;

namespace SetPaint
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
        #region CONSTANTS
        private const int kLineIndex = 0;
        private const int kRectangleIndex = 1;
        private const int kEllipseIndex = 2;

        // For the shape outline pattern - dash and gap
        private const double kDashLength = 5;
        private const double kDashGap = 5;

        private const string kLblMousePosition = "Mouse Position: ";
        #endregion 

        Point ptBegin;              //< Start point (of when mouse is down)
        Point ptEnd;                //< End point (of when mouse is up)
        Point ptCurrent;            //< Current point (of where the mouse is)
        double x1 = 0.0;            //< The x-coordinate for the 1st point
        double y1 = 0.0;            //< The y-coordinate for the 1st point
        double x2 = 0.0;            //< The x-coordinate for the 2nd point
        double y2 = 0.0;            //< The y-coordinate for the 2nd point
        DoubleCollection dashPattern;
        Brush lineColor;
        Brush fillColor;
        double lineThickness;

        private int currentShapeCount = 0;      //< For checking if save condition is applicable
        private int previousShapeCount = 0;     //< Tracking the unmodified shape count

        /* Boolean flags */
        private bool isLine = false;        //< bool flag indicating the user is drawing a line
        private bool isRectangle = false;   //< bool flag indicating the user is drawing a rectangle
        private bool isEllipse = false;     //< bool flag indicating the user is drawing an ellipse
        private bool isDrawing = false;     //< bool flag indicating if user is drawing something
        private bool needSaving = false;    //< bool flag indicating if save is needed

        private string currentFileName = "Untitled";
        public MainWindow()
		{
			InitializeComponent();
            UpdateName();

            transparentToolStripMenuItem.IsCheckable = true;
            transparentToolStripMenuItem.IsChecked = true;
            fillToolStripMenuItem.IsCheckable = true;

            DrawArea.Background = new SolidColorBrush(Colors.White);
            lineColor = Brushes.Blue;
            fillColor = null;
            lineThickness = 1.0;

            // To use dash outlines - create DoubleCollection
            // 0th index is the length of a dash
            // 1th index is the length of a gap between two dashes
            dashPattern = new DoubleCollection();
            dashPattern.Add(kDashLength);
            dashPattern.Add(kDashGap);
        }

        /// <summary>
        /// Update the application title.
        /// </summary>
        /// <param name="tmp">The new name for the title.</param>
        private void UpdateName()
        {

            string tmp = currentFileName + " - SetPaint";
            this.Title = tmp;
        }


        #region MENU_ITEMS
        /// <summary>
        /// Closes the application through the Exit menu item.
        /// </summary>
        /// <param name="sender">The control that was clicked.</param>
        /// <param name="e">The event arguments.</param>
        private void exitToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
            DrawArea.Children.Clear();
			Clipboard.Clear();
			Application.Current.Shutdown();
		}

        /// <summary>
        /// Show the About Box to the user.
        /// </summary>
        /// <param name="sender">The menu item that was clicked.</param>
        /// <param name="e">The event arguments.</param>
        private void aboutToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CustomAboutBox about = new CustomAboutBox();
            about.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            about.ShowDialog();
        }

        /// <summary>
        /// Clears the canvas of any shapes or lines. It first asks
        /// the user if they want to erase the canvas or not.
        /// </summary>
        /// <param name="sender">The control that was clicked.</param>
        /// <param name="e">The even arguments.</param>
        private void eraseToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result;
            string text = "Are you sure you want to clear the canvas?";
            string caption = "Erase?";
            result = MessageBox.Show(this, text, caption, MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    // Clear the canvas area
                    DrawArea.Children.Clear();
                    break;

                case MessageBoxResult.No:
                    // Leave canvas alone
                    break;
            }
        }
        #endregion


        #region DRAWING
        /// <summary>
        /// Get the starting position of the mouse click. The
        /// beginning point is used to draw an initial Shape that
        /// is later modified to "rubber band". The Shapes are drawn
        /// at MouseLeftButtonDown and uses the beginning point as a
        /// reference. As well, the shapes are outlined - dashed.
        /// <para/>
        /// </summary>
        /// <param name="sender">The control that has the mouse click.</param>
        /// <param name="e">The mouse button events.</param>
        private void DrawArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Get the start point of the left mouse click is down
            ptBegin = e.GetPosition(DrawArea);
            // The user is drawing
            isDrawing = true;

            /*
             * The rubberbanding initialization starts here. What these 
             * code statements are doing is specifying a Shape at a position.
             * When it is drawn, it is just a point (non-existent). This enables
             * the Shape to be added to a collection. This collection plays a
             * for "retrieving" that Shape - thus it allows us to rubber band...
             */
            if (isLine)
            {
                Line line = DrawLine(ptBegin, ptBegin, lineColor, lineThickness);
                line.StrokeDashArray = dashPattern;
                DrawArea.Children.Add(line);
            }
            else if (isRectangle)
            {
                Rectangle rect = DrawRectangle(ptBegin, ptBegin, lineColor, lineThickness, fillColor);
                rect.StrokeDashArray = dashPattern;
                DrawArea.Children.Add(rect);
            }
            else if (isEllipse)
            {
                Ellipse ellipse = DrawEllipse(ptBegin, ptBegin, lineColor, lineThickness, fillColor);
                ellipse.StrokeDashArray = dashPattern;
                DrawArea.Children.Add(ellipse);
            }
        }

        /// <summary>
        /// When the mouse is moving around in the canvas, the current
        /// position of the mouse is recorded. If the user has the
        /// left mouse button pressed and is drawing, then we rubber band
        /// the Shape. The Shape is updated to a new position (which is the
        /// current position).
        /// </summary>
        /// <param name="sender">The control that has the mouse click.</param>
        /// <param name="e">The mouse button events.</param>
        private void DrawArea_MouseMove(object sender, MouseEventArgs e)
        {
            // Is the left mouse button pressed and held?
            // Is the user drawing something?
            if ((e.LeftButton == MouseButtonState.Pressed) && (isDrawing))
            {
                // Get the current position of the mouse
                ptCurrent = e.GetPosition(DrawArea);

                double x = ptCurrent.X;
                double y = ptCurrent.Y;
                string position = kLblMousePosition + x.ToString("0")
                    + ", " + y.ToString("0") + "px";
                lblMousePosition.Text = position;

                /*
                 * Here is where the rubber banding comes into play. For these Shapes
                 * the concept of rubber banding is accomplished by "updating"; taking
                 * the current position of the mouse's X and Y. How we "retrieve" the
                 * Shape is by calling the LastOrDefault() in the collection. This will
                 * get the last Shape that was added to the collection - which was drawn
                 * in the MouseLeftButtonDown event.
                 */
                if (isDrawing)
                {
                    if (isLine)
                    {
                        // When the line was drawn initially, it was added to a
                        // collection. Thus we could "retrieve" that line and thus
                        // we can manipulate it.
                        Line line = DrawArea.Children.OfType<Line>().LastOrDefault();
                        if (line != null)
                        {
                            // For a line, update the end-point of the line with the
                            // coordinates of the current mouse position.
                            line.X2 = ptCurrent.X;
                            line.Y2 = ptCurrent.Y;
                        }
                    }
                    else if (isRectangle)
                    {
                        Rectangle rect = DrawArea.Children.OfType<Rectangle>().LastOrDefault();
                        if (rect != null)
                        {
                            SpecifySize(rect, ptBegin.X, ptCurrent.X, ptBegin.Y, ptCurrent.Y);
                            SpecifyPosition(rect, ptBegin.X, ptCurrent.X, ptBegin.Y, ptCurrent.Y);
                        }
                    }
                    else if (isEllipse)
                    {
                        Ellipse ellipse = DrawArea.Children.OfType<Ellipse>().LastOrDefault();
                        if (ellipse != null)
                        {
                            SpecifySize(ellipse, ptBegin.X, ptCurrent.X, ptBegin.Y, ptCurrent.Y);
                            SpecifyPosition(ellipse, ptBegin.X, ptCurrent.X, ptBegin.Y, ptCurrent.Y);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Once the mouse button is up, the Shape is drawn permanently. First,
        /// the end position (of where the mouse button is up) is recorded. 
        /// Then the rubber banding shape is retrieved and deleted. Finally the
        /// Shape is drawn with the permanent positions.
        /// </summary>
        /// <param name="sender">The control that has the mouse click.</param>
        /// <param name="e">The mouse button events.</param>
        private void DrawArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Once mouse left is up, clear the mouse position
            lblMousePosition.Text = kLblMousePosition;

            // Get the end point of when the left mouse click is up
            ptEnd = e.GetPosition(DrawArea);

            bool samePosition = false;

            // Coordinates for permanent shape
            x1 = ptBegin.X;
            y1 = ptBegin.Y;
            x2 = ptEnd.X;
            y2 = ptEnd.Y;
            isDrawing = false;

            if (isLine)
            {
                //Remove the old line (that was rubber banding)
                Line oldLine = DrawArea.Children.OfType<Line>().LastOrDefault();
                if (oldLine != null)
                {
                    DrawArea.Children.Remove(oldLine);
                }
                 
                samePosition = CheckPosition(x1, x2, y1, y2);
                if (!samePosition)
                {
                    // Draw a line
                    Line line = DrawLine(ptBegin, ptEnd, lineColor, lineThickness);
                    // Display the line to the canvas
                    DrawArea.Children.Add(line);
                }
            }
            else if (isRectangle)
            {
                // Remove the old rectangle (that was rubber banding)
                Rectangle oldRect = DrawArea.Children.OfType<Rectangle>().LastOrDefault();
                if (oldRect != null)
                {
                    DrawArea.Children.Remove(oldRect);
                }

                samePosition = CheckPosition(x1, x2, y1, y2);
                if (!samePosition)
                {
                    // Drawing the permanent rectangle
                    Rectangle rect = DrawRectangle(ptBegin, ptEnd, lineColor, lineThickness, fillColor);
                    // Display the rectangle to the canvas
                    DrawArea.Children.Add(rect);
                }
            }
            else if (isEllipse)
            {
                // Remove the old ellipse (that was rubber banding)
                Ellipse oldEllipse = DrawArea.Children.OfType<Ellipse>().LastOrDefault();
                if (oldEllipse != null)
                {
                    DrawArea.Children.Remove(oldEllipse);
                }

                samePosition = CheckPosition(x1, x2, y1, y2);
                if (!samePosition)
                {
                    // Draw the ellipse to the screen
                    Ellipse ellipse = DrawEllipse(ptBegin, ptEnd, lineColor, lineThickness, fillColor);
                    // Display the rectangle to the canvas
                    DrawArea.Children.Add(ellipse);
                }
            }

            currentShapeCount = DrawArea.Children.Count;
            CanvasChanged();
        }

        /// <summary>
        /// The user's mouse has left the canvas area. If the user is
        /// still drawing, change the Shape's line border style to
        /// a solid line.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawArea_MouseLeave(object sender, MouseEventArgs e)
        {
            Canvas canvas = (Canvas)sender;

            if (isDrawing)
            {
                if (isLine)
                {
                    Line outsideLine = canvas.Children.OfType<Line>().LastOrDefault();
                    if (outsideLine != null)
                    {
                        // Change the line stroke to solid, not dashed.
                        outsideLine.StrokeDashArray = null;
                    }
                }
                else if (isRectangle)
                {
                    Rectangle outsideRect = canvas.Children.OfType<Rectangle>().LastOrDefault();
                    if (outsideRect != null)
                    {
                        // Change the line stroke to solid, not dashed.
                        outsideRect.StrokeDashArray = null;
                    }
                }
                else if (isEllipse)
                {
                    Ellipse outsideEllipse = canvas.Children.OfType<Ellipse>().LastOrDefault();
                    if (outsideEllipse != null)
                    {
                        // Change the line stroke to solid, not dashed.
                        outsideEllipse.StrokeDashArray = null;
                    }
                }
            }
        }
        #endregion


        #region Radio Button Events
        /// <summary>
        /// The line radio button was clicked.
        /// </summary>
        /// <param name="sender">The radio button control.</param>
        /// <param name="e">The click event.</param>
        private void rbttnLine_Checked(object sender, RoutedEventArgs e)
        {
            ToggleRadioButton(true, kLineIndex);
        }

        /// <summary>
        /// The rectangle radio button was clicked.
        /// </summary>
        /// <param name="sender">The radio button control.</param>
        /// <param name="e">The click event.</param>
        private void rbttnRectangle_Checked(object sender, RoutedEventArgs e)
        {
            ToggleRadioButton(true, kRectangleIndex);
        }

        /// <summary>
        /// The ellipse radio button was clicked.
        /// </summary>
        /// <param name="sender">The radio button control.</param>
        /// <param name="e">The click event.</param>
        private void rbttnEllipse_Checked(object sender, RoutedEventArgs e)
        {
            ToggleRadioButton(true, kEllipseIndex);
        }
        #endregion


        #region Drawing Shapes
        /// <summary>
        /// Setting up the configurations for a line.
        /// </summary>
        /// <param name="start">The start point (at which the mouse click is down).</param>
        /// <param name="end">The end point (at which the mouse click is up).</param>
        /// <param name="stroke">The line border colour.</param>
        /// <param name="thickness">The thickness outline of the shape.</param>
        /// <returns>The line.</returns>
        private Line DrawLine(Point start, Point end, Brush stroke, double thickness)
        {
            Line line = new Line();
            line.Stroke = stroke;
            line.StrokeThickness = thickness;
            line.X1 = start.X;
            line.X2 = end.X;
            line.Y1 = start.Y;
            line.Y2 = end.Y;

            return line;
        }

        /// <summary>
        /// Setting up the configurations for the Rectangle.
        /// </summary>
        /// <param name="start">The start point (at which the mouse click is down).</param>
        /// <param name="end">The end point (at which the mouse click is up).</param>
        /// <param name="stroke">The line border colour.</param>
        /// <param name="thickness">The thickness outline of the rectangle.</param>
        /// <param name="fill">Interior shape colour.</param>
        /// <returns>The rectangle shape.</returns>
        private Rectangle DrawRectangle(Point start, Point end, Brush stroke, double thickness, Brush fill = null)
        {
            Rectangle rect = new Rectangle();

            rect.Stroke = stroke;
            rect.StrokeThickness = thickness;
            rect.Fill = fill;
            SpecifySize(rect, start.X, end.X, start.Y, end.Y);
            SpecifyPosition(rect, start.X, end.X, start.Y, end.Y);
            
            return rect;
        }

        /// <summary>
        /// Setting up the configurations for the Ellipse.
        /// </summary>
        /// <param name="start">The start point (at which the mouse click is down).</param>
        /// <param name="end">The end point (at which the mouse click is up).</param>
        /// <param name="stroke">The line border colour.</param>
        /// <param name="thickness">The thickness outline of the ellipse.</param>
        /// <param name="fill">Interior shape colour.</param>
        /// <returns>Ellipse - An ellipse shape.</returns>
        private Ellipse DrawEllipse(Point start, Point end, Brush stroke, double thickness, Brush fill = null)
        {
            Ellipse ellipse = new Ellipse();

            ellipse.Stroke = stroke;
            ellipse.StrokeThickness = thickness;
            ellipse.Fill = fill;
            SpecifySize(ellipse, start.X, end.X, start.Y, end.Y);
            SpecifyPosition(ellipse, start.X, end.X, start.Y, end.Y);

            return ellipse;
        }

        /// <summary>
        /// Setting the width (x-axis) and height (y-height) of a shape.
        /// </summary>
        /// <param name="temp">Generic Shape object that sets the size of the passed in shape.</param>
        /// <param name="x1">The starting x-point.</param>
        /// <param name="x2">The ending x-point.</param>
        /// <param name="y1">The starting y-point.</param>
        /// <param name="y2">The ending y-point.</param>
        private void SpecifySize(Shape temp, double x1, double x2, double y1, double y2)
        {
            // (x1, y1) is the origin point. We base it upon that position

            if (x2 >= x1)
            {
                // x2 is greater than x1, then it is generally POSITIVELY x
                // In this case: positive width
                temp.Width = x2 - x1;
            }
            else if (x2 <= x1)
            {
                // x2 is less than x1, then it is generally NEGATIVE x
                // In this case: pt1 is greater than pt2
                // Get current point to draw from Current to Start
                temp.Width = x1 - x2;
            }

            if (y2 >= y1)
            {
                // y2 is greater than y1, then it is generally POSITIVE y
                // In this case: positive height
                temp.Height = y2 - y1;
            }
            else if (y2 <= y1)
            {
                // y2 is less than y1, then it is generally NEGATIVE y
                // In this case: mouse position less than start point (on y-axis)...
                // Get current point to draw from Current to Start
                temp.Height = y1 - y2;
            }
        }

        /// <summary>
        /// Specifying, relative to the canvas area, where the shape
        /// will be drawn. From the Left (x-axis) and Top (y-axis) of the canvas,
        /// the shape can be drawn relative to the specified points.
        /// </summary>
        /// <param name="temp">Generic Shape object is used to specify relative position.</param>
        /// <param name="x1">The starting x-point.</param>
        /// <param name="x2">The ending x-point.</param>
        /// <param name="y1">The starting y-point.</param>
        /// <param name="y2">The ending y-point.</param>
        private void SpecifyPosition(Shape temp, double x1, double x2, double y1, double y2)
        {
            /* Cartesian Plane*/
            // Quadrant 1 (+x, +y)
            if (x2 >= x1 && y2 >= y1)
            {
                /* In this case, we DON'T have to move the origin */
                // SetLeft is the x-axis
                Canvas.SetLeft(temp, x1);
                // SetTop is the y-axis
                Canvas.SetTop(temp, y1);
            }
            // Quadrant 2 (+x, -y)
            else if (x2 >= x1 && y2 <= y1)
            {
                /* In this case, origin moves in negative y-axis */
                Canvas.SetLeft(temp, x1);
                Canvas.SetTop(temp, y2);
            }
            // Quadrant 3 (-x, -y)
            else if (x2 <= x1 && y2 <= y1)
            {
                /* In this case, origin moves in negative x and y axis */
                Canvas.SetLeft(temp, x2);
                Canvas.SetTop(temp, y2);
            }
            // Quadrant 4 (-x, +y)
            else if (x2 <= x1 && y2 >= y1)
            {
                Canvas.SetLeft(temp, x2);
                Canvas.SetTop(temp, y1);
            }
        }
        #endregion


        #region SetPaint Utilities
        /// <summary>
        /// Determines whether the user is drawing a shape
        /// by toggling between boolean flags. It is dependent
        /// on the radio button selection.
        /// </summary>
        /// <param name="status">Enable the drawing of that selected shape.</param>
        /// <param name="index">
        /// The radio button option that was selected (line, rectangle, ellipse).
        /// </param>
        private void ToggleRadioButton(bool status, int index)
        {
            switch (index)
            {
                case kLineIndex:
                    isLine = status;
                    isRectangle = !status;
                    isEllipse = !status;
                    break;

                case kRectangleIndex:
                    isLine = !status;
                    isRectangle = status;
                    isEllipse = !status;
                    break;

                case kEllipseIndex:
                    isLine = !status;
                    isRectangle = !status;
                    isEllipse = status;
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Check the start and end point of the mouse position.
        /// </summary>
        /// <param name="x1">The starting x-point.</param>
        /// <param name="x2">The ending x-point.</param>
        /// <param name="y1">The starting y-point.</param>
        /// <param name="y2">The ending y-point.</param>
        /// <returns>bool - true if mouse is at same position or false if mouse has moved.</returns>
        private bool CheckPosition(double x1, double x2, double y1, double y2)
        {
            bool retBool = false;

            if (x1.Equals(x2) && y1.Equals(y2))
            {
                retBool = true;
            }

            return retBool;
        }

        /// <summary>
        /// Whenever shapes are drawn and added to the Canvas' container,
        /// check for any changes. If there are changes, then the user's
        /// drawing needs to be saved.
        /// </summary>
        private void CanvasChanged()
        {
            // If a new shape is drawn, it is added to the
            // Canvas count. So, check to previous shape count
            // and the current shape count to determine if a 
            // save is really needed.
            if (currentShapeCount > previousShapeCount)
            {
                needSaving = true;
            }
        }
        #endregion


        #region File IO: Save and Open
        /// <summary>
        /// Open the image file and display it to the canvas. An OpenFileDialog
        /// is used to open an image file to the canvas
        /// </summary>
        /// <param name="sender">The menu item that was clicked.</param>
        /// <param name="e">The mouse click event.</param>
        private void open_click(object sender, RoutedEventArgs e)
        {
            isDrawing = false;

            if (!isDrawing)
            {
                OpenFileDialog openfile = new OpenFileDialog();
                openfile.DefaultExt = ".png";
                openfile.Filter = "Image documents (.png)|*.png";

                if (openfile.ShowDialog() == true)
                {
                    string filename = openfile.FileName;
                    currentFileName = openfile.SafeFileName;
                    UpdateName();
                    needSaving = false;
                    currentShapeCount = 0;
                    previousShapeCount = 0;

                    DrawArea.Children.Clear();
                    DrawArea.Background = new SolidColorBrush(Colors.White);

                    ImageBrush brush = new ImageBrush();
                    BitmapImage bmi = new BitmapImage();

                    bmi.BeginInit();
                    bmi.CacheOption = BitmapCacheOption.OnLoad;
                    bmi.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bmi.UriSource = new Uri(filename, UriKind.RelativeOrAbsolute);
                    bmi.EndInit();

                    brush.ImageSource = bmi;
                    DrawArea.Background = brush;
                }

            }
        }

        /// <summary>
        /// Save the canvas as an image. Uses a SaveFileDialog to save the
        /// file. A RenderTargetBitmap is used to render the area of what
        /// will be saved. A BitmapEncoder is used to encode the area
        /// into an image.
        /// </summary>
        /// <param name="sender">The menu item that was clicked.</param>
        /// <param name="e">The mouse click event.</param>
        private void save_click(object sender, RoutedEventArgs e)
        {
            isDrawing = false;

            SaveFileDialog savefile = new SaveFileDialog();

            savefile.DefaultExt = ".png";
            savefile.Filter = "Image documents (.png)|*.png";
            savefile.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            if (savefile.ShowDialog() == true)
            {
                string fileName = savefile.FileName;
                currentFileName = savefile.SafeFileName;
                UpdateName();

                RenderTargetBitmap rtb = new RenderTargetBitmap((int)DrawArea.RenderSize.Width,
                  (int)DrawArea.RenderSize.Height, 96d, 96d, PixelFormats.Default);

                rtb.Render(DrawArea);

                BitmapEncoder pngEncoder = new PngBitmapEncoder();
                pngEncoder.Frames.Add(BitmapFrame.Create(rtb));

                //save to memory stream
                MemoryStream ms = new MemoryStream();
                pngEncoder.Save(ms);

                try
                {
                    File.WriteAllBytes(fileName, ms.ToArray());
                    // After writing to file, update the application title
                    currentFileName = savefile.SafeFileName;
                    UpdateName();
                    needSaving = false;
                    previousShapeCount = currentShapeCount;
                }
                catch (IOException ex)
                {
                    string err_msg = ex.Message;
                    string text = "Could not save the file. Please try again or save it as another name.";
                    string caption = "Error. Saving file";
                    MessageBox.Show(text, caption, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    ms.Close();
                }
            }
        }

        /// <summary>
        /// Clear out the entire canvas area. Restart as a new canvas.
        /// </summary>
        /// <param name="sender">The menu item that was clicked.</param>
        /// <param name="e">The mouse click event.</param>
        private void newToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (needSaving)
            {
                string _msg = "Do you want to save changes to " + currentFileName + "?";
                string caption = "Save changes?";
                MessageBoxResult result =  MessageBox.Show(_msg, caption, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);
                if (result == MessageBoxResult.Yes)
                {
                    save_click(sender, e);
                }
            }

            DrawArea.Background = new SolidColorBrush(Colors.White);
            if (DrawArea.Children.Count > 0)
            {
                DrawArea.Children.Clear();
            }

            currentFileName = "Untitled";
            UpdateName();

            currentShapeCount = 0;
            previousShapeCount = 0;
            needSaving = false;
        }
        #endregion


        #region Colour Picking
        /// <summary>
        /// Change the line stroke color.
        /// </summary>
        /// <param name="sender">The menu item control.</param>
        /// <param name="e">The click event.</param>
        private void lineColorToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Change the line color
            lineColor = (Brush)ColorPicker();
        }

        /// <summary>
        /// Make the shape fill transparent.
        /// </summary>
        /// <param name="sender">The menu item control.</param>
        /// <param name="e">The click event.</param>
        private void transparentToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Enable the check marks to indicate to the user the fill is transparent
            transparentToolStripMenuItem.IsChecked = true;
            fillToolStripMenuItem.IsChecked = false;

            // There is no fill color
            fillColor = null;
        }

        /// <summary>
        /// Make the shape fill be a color of the user's choosing.
        /// </summary>
        /// <param name="sender">The menu item control.</param>
        /// <param name="e">The click event.</param>
        private void fillToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Enable the check marks to indicate to the user the fill is color
            transparentToolStripMenuItem.IsChecked = false;
            fillToolStripMenuItem.IsChecked = true;

            // Use the brush color for the fill color
            fillColor = (Brush)ColorPicker();
        }

        /// <summary>
        /// The color dialog box is opened. Allows the user to choose pre-defined
        /// colors or custom colors. Once the color is chosen, convert to ARGB values.
        /// </summary>
        /// <returns>The color the user has chosen</returns>
        private SolidColorBrush ColorPicker()
        {
            SolidColorBrush brushColor = new SolidColorBrush();

            System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();
            System.Windows.Forms.DialogResult result = colorDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                byte alpha = colorDialog.Color.A;
                byte red = colorDialog.Color.R;
                byte green = colorDialog.Color.G;
                byte blue = colorDialog.Color.B;

                Color color = Color.FromArgb(alpha, red, green, blue);
                brushColor.Color = color;
            }

            return brushColor;
        }
        #endregion


        #region Line Thickness
        /// <summary>
        /// Change the line thickness of lines. Once menu item is clicked,
        /// a Line Thickness dialog box is shown. The user can choose a line thickness
        /// and it will be reflected in the main window.
        /// </summary>
        /// <param name="sender">The menu item that was clicked.</param>
        /// <param name="e">The click event.</param>
        private void linkThicknessToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LineThicknessDialog lineDialog = new LineThicknessDialog(this, lineThickness);
            // Center the dialog to the parent window
            lineDialog.ShowDialog();

            lineThickness = lineDialog.LineThickness;
        }
        #endregion
    }
}
