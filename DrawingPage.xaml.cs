using Microsoft.Win32;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Scribble
{
    /// <summary>
    /// Interaction logic for DrawingPage.xaml
    /// </summary>
    public partial class DrawingPage : UserControl
    {
        List<Stroke> strokes = new List<Stroke>();
        Stroke? currentStroke;
        Polyline? polyline;
        Brush PenColor = Brushes.Black;
        public int PenColorNumber = 7;
        public double PenWidth = 2;
        bool IsDrawingStroke = false;
        public bool isDirty = false;
        public string ScribbleFileName = "";
        public bool EnableUndo = false;
        public bool EnableRedo = false;

        public DrawingPage()
        {
            InitializeComponent();
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.EnableUndo(false);
                mainWindow.EnableRedo(false);
                mainWindow.EnableSave(false);
            }
        }

        #region mouse events
        private void canvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(canvas);
            // Capture the mouse to ensure we track it even if it leaves the window
            canvas.CaptureMouse();

            // create polyline
            polyline = new Polyline();
            polyline.Stroke = ColorPicker.GetBrushColor(PenColorNumber);
            polyline.StrokeThickness = PenWidth;
            polyline.StrokeLineJoin = PenLineJoin.Round;
            polyline.StrokeStartLineCap = PenLineCap.Round;
            polyline.StrokeEndLineCap = PenLineCap.Round;
            polyline.Points.Add(p);
            canvas.Children.Add(polyline);

            // create Stroke
            currentStroke = new Stroke();
            currentStroke.color = ColorPicker.GetBrushColor(PenColorNumber);
            currentStroke.width = PenWidth;
            currentStroke.points.Add(p);

            IsDrawingStroke = true;
        }

        private void canvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (IsDrawingStroke)
            {
                Point p = e.GetPosition(canvas);

                // draw a line
                if (polyline != null)
                    polyline.Points.Add(p);
                if (currentStroke != null)
                    currentStroke.points.Add(p);
            }
        }

        private void canvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (IsDrawingStroke)
            {
                Point p = e.GetPosition(canvas);

                // draw a line
                if (polyline != null)
                    polyline.Points.Add(p);
                if (currentStroke != null)
                    currentStroke.points.Add(p);

                if (currentStroke != null)
                {
                    strokes.Add(currentStroke);
                    ClearRedo();
                    SetUndoRedoState();
                }
                isDirty = true;
                SetSaveState();
            }
            IsDrawingStroke = false;
            canvas.ReleaseMouseCapture();
        }
        #endregion
        #region file menu events
        public void Open()
        {
            if (isDirty)
            {
                MessageBoxResult result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save before continuing?",
                    "Save changes?",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    Save(); // your existing save logic
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return; // abort closing entirely
                }
                // MessageBoxResult.No falls through — close without saving
            }

            // save to file
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Scribble Files|*.scribble|All Files|*.*";
            dlg.DefaultExt = ".scribble";
            if (dlg.ShowDialog() == true)
            {
                bool success = ReadJson(dlg.FileName);
                if (success)
                {
                    ScribbleFileName = dlg.FileName;
                    SetUndoRedoState();
                }
            }
        }

        public void OpenFile(string fileName)
        {
            if (isDirty)
            {
                MessageBoxResult result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save before continuing?",
                    "Save changes?",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    Save(); // your existing save logic
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return; // abort closing entirely
                }
                // MessageBoxResult.No falls through — close without saving
            }

            bool success = ReadJson(fileName);
            if (success)
            {
                ScribbleFileName = fileName;
                SetUndoRedoState();
            }
        }

        private bool ReadJson(string fileName)
        {
            List<StrokeData>? strokesData = null;

            try
            {
                string json = File.ReadAllText(fileName);
                strokesData = JsonSerializer.Deserialize<List<StrokeData>>(json);
                if (strokesData == null)
                {
                    MessageBox.Show("That file doesn't contain valid Scribble data.", "Open Failed");
                    return false;
                }
            }
            catch (JsonException)
            {
                MessageBox.Show("That file doesn't appear to be a valid Scribble file.", "Open Failed");
                return false;
            }
            catch (IOException)
            {
                MessageBox.Show("Unable to read that file.", "Open Failed");
                return false;
            }

            if (strokesData != null)
            {
                strokes.Clear();
                for (int i = 0; i < strokesData.Count; i++)
                {
                    Stroke newStroke = new Stroke();
                    newStroke.width = strokesData[i].Width;
                    strokes.Add(newStroke);
                    Color color = Color.FromArgb(strokesData[i].ColorA, strokesData[i].ColorR, strokesData[i].ColorG, strokesData[i].ColorB);
                    Brush brush = new SolidColorBrush(color);
                    newStroke.color = brush;
                    for (int j = 0; j < strokesData[i].Points.Count; j++)
                    {
                        Point p = new Point();
                        p.X = strokesData[i].Points[j].X;
                        p.Y = strokesData[i].Points[j].Y;
                        newStroke.points.Add(p);
                    }
                    strokes.Add(newStroke);
                }
            }

            Redraw();
            isDirty = false;
            SetSaveState();
            return true;
        }

        public void Save()
        {
            if (ScribbleFileName == "")
                SaveAs();
            else
            {
                string json = GetJson();
                File.WriteAllText(ScribbleFileName, json);
                isDirty = false;
                SetSaveState();
            }
        }

        public void SaveAs()
        {
            // save to file
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Scribble Files|*.scribble";
            dlg.DefaultExt = ".scribble";
            if (dlg.ShowDialog() == true)
            {
                ScribbleFileName = dlg.FileName;
                string json = GetJson();
                File.WriteAllText(ScribbleFileName, json);

                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    mainWindow.UpdateRecentFiles(ScribbleFileName);
                }
                isDirty = false;
                SetSaveState();
            }
        }

        private string GetJson()
        {
            // create stroke data
            List<StrokeData> strokesData = new List<StrokeData>();
            for (int i = 0; i < strokes.Count; i++)
            {
                if (strokes[i].IsVisible)
                {
                    StrokeData newStroke = new StrokeData();
                    newStroke.Width = strokes[i].width;
                    var solidBrush = (SolidColorBrush)strokes[i].color;
                    newStroke.ColorA = solidBrush.Color.A;
                    newStroke.ColorR = solidBrush.Color.R;
                    newStroke.ColorG = solidBrush.Color.G;
                    newStroke.ColorB = solidBrush.Color.B;
                    for (int j = 0; j < strokes[i].points.Count; j++)
                    {
                        PointData newPoint = new PointData();
                        newPoint.X = strokes[i].points[j].X;
                        newPoint.Y = strokes[i].points[j].Y;
                        newStroke.Points.Add(newPoint);
                    }
                    strokesData.Add(newStroke);
                }
            }
            string json = JsonSerializer.Serialize(strokesData);
            return json;
        }

        public void Export_Click()
        {
            // create a bitmap
            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)canvas.ActualWidth, (int)canvas.ActualHeight,
                96, 96, PixelFormats.Pbgra32);
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(canvas);
                dc.DrawRectangle(vb, null, new Rect(0, 0, canvas.ActualWidth, canvas.ActualHeight));
            }
            rtb.Render(dv);

            // save to file
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "PNG Image|*.png";
            dlg.DefaultExt = ".png";
            if (dlg.ShowDialog() == true)
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                using (FileStream fs = new FileStream(dlg.FileName, FileMode.Create))
                {
                    encoder.Save(fs);
                }
            }
        }

        public void Print_Click()
        {
            // create a bitmap
            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)canvas.ActualWidth, (int)canvas.ActualHeight,
                96, 96, PixelFormats.Pbgra32);

            // render the canvas into the bitmap
            DrawingVisual dvCapture = new DrawingVisual();
            using (DrawingContext dc = dvCapture.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(canvas);
                dc.DrawRectangle(vb, null, new Rect(0, 0, canvas.ActualWidth, canvas.ActualHeight));
            }
            rtb.Render(dvCapture);

            // print the bitmap
            PrintDialog dlg = new PrintDialog();
            if (dlg.ShowDialog() == true)
            {
                DrawingVisual dvPrint = new DrawingVisual();

                // 1 inch = 25.4mm, 1 inch = 96 pixels
                double mmToPixels = 96.0 / 25.4;

                double marginTop = 15 * mmToPixels;
                double marginBottom = 15 * mmToPixels;
                double marginLeft = 15 * mmToPixels;
                double marginRight = 15 * mmToPixels;

                double printableWidth = dlg.PrintableAreaWidth - marginLeft - marginRight;
                double printableHeight = dlg.PrintableAreaHeight - marginTop - marginBottom;

                double canvasAspect = canvas.ActualWidth / canvas.ActualHeight;
                double pageAspect = printableWidth / printableHeight;

                double printWidth, printHeight;

                if (canvasAspect > pageAspect)
                {
                    printWidth = printableWidth;
                    printHeight = printWidth / canvasAspect;
                }
                else
                {
                    printHeight = printableHeight;
                    printWidth = printHeight * canvasAspect;
                }

                // center within the margins
                double offsetX = marginLeft + (printableWidth - printWidth) / 2;
                double offsetY = marginTop + (printableHeight - printHeight) / 2;

                using (DrawingContext dc = dvPrint.RenderOpen())
                {
                    dc.DrawImage(rtb, new Rect(offsetX, offsetY, printWidth, printHeight));
                }

                dlg.PrintVisual(dvPrint, "Scribble");
            }
        }
        #endregion
        #region edit menu events
        public void Undo_Click()
        {
            if (strokes.Count > 0)
            {
                for (int i = strokes.Count - 1; i >= 0; i--)
                {
                    if (strokes[i].IsVisible)
                    {
                        strokes[i].IsVisible = false;
                        isDirty = true;
                        SetSaveState();
                        Redraw();
                        SetUndoRedoState();
                        return;
                    }
                }
            }
        }

        public void Redo_Click()
        {
            if (strokes.Count > 0)
            {
                for (int i = 0; i < strokes.Count; i++)
                {
                    if (!strokes[i].IsVisible)
                    {
                        strokes[i].IsVisible = true;
                        isDirty = true;
                        SetSaveState();
                        Redraw();
                        SetUndoRedoState();
                        return;
                    }
                }
            }

        }

        public void Copy_Click()
        {
            // create a bitmap
            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)canvas.ActualWidth, (int)canvas.ActualHeight,
                96, 96, PixelFormats.Pbgra32);
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(canvas);
                dc.DrawRectangle(vb, null, new Rect(0, 0, canvas.ActualWidth, canvas.ActualHeight));
            }
            rtb.Render(dv);

            // Copy to clipboard
            Clipboard.SetImage(rtb);

            // Restore window layout (copying can mess it up)
            //InvalidateWindowLayout();
        }

        public void ClearAll_Click()
        {
            if (isDirty)
            {
                MessageBoxResult result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save before continuing?",
                    "Save changes?",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    Save(); // your existing save logic
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return; // abort closing entirely
                }
                // MessageBoxResult.No falls through — close without saving
            }

            canvas.Children.Clear();
            strokes.Clear();
            isDirty = false;
            SetSaveState();
            SetUndoRedoState();
        }
        #endregion
        private void Redraw()
        {
            canvas.Children.Clear();
            for (int i = 0; i < strokes.Count; i++)
            {
                if (strokes[i].IsVisible)
                {
                    // create polyline
                    polyline = new Polyline();
                    polyline.Stroke = strokes[i].color;
                    polyline.StrokeThickness = strokes[i].width;
                    polyline.StrokeLineJoin = PenLineJoin.Round;
                    polyline.StrokeStartLineCap = PenLineCap.Round;
                    polyline.StrokeEndLineCap = PenLineCap.Round;
                    for (int j = 0; j < strokes[i].points.Count; j++)
                        polyline.Points.Add(strokes[i].points[j]);
                    canvas.Children.Add(polyline);
                }
            }
        }

        private void SetUndoRedoState()
        {
            EnableUndo = false;
            EnableRedo = false;
            for (int i = 0; i < strokes.Count; i++)
            {
                if (strokes[i].IsVisible == true)
                    EnableUndo = true;
                else
                    EnableRedo = true;
            }

            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.EnableUndo(EnableUndo);
                mainWindow.EnableRedo(EnableRedo);
            }
        }

        private void SetSaveState()
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.EnableSave(isDirty);
            }
        }

        private void ClearRedo()
        {
            int i = strokes.Count - 1;
            while (i >= 0)
            {
                if (!strokes[i].IsVisible)
                    strokes.RemoveAt(i);
                i--;
            }
        }
    }

    class Stroke
    {
        public Brush color = Brushes.Black;
        public double width = 1;
        public bool IsVisible = true;
        public List<Point> points = new List<Point>();
    }

    public class StrokeData
    {
        public double Width { get; set; }
        public byte ColorA { get; set; }
        public byte ColorR { get; set; }
        public byte ColorG { get; set; }
        public byte ColorB { get; set; }
        public List<PointData> Points { get; set; } = new List<PointData>();
    }

    public class PointData
    {
        public double X { get; set; }
        public double Y { get; set; }
    }
}
