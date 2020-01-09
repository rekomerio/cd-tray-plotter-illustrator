using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace VectorDrawingApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public class Coordinate
    {
        public int x = 0, y = 0, z = 0;
        public char identifier;

        public override string ToString()
        {
            string sX = Convert.ToString(x, 16);
            sX = sX.Length < 2 ? "0" + sX : sX;

            string sY = Convert.ToString(y, 16);
            sY = sY.Length < 2 ? "0" + sY : sY;

            string sZ = Convert.ToString(z, 16);
            sZ = sZ.Length < 2 ? "0" + sZ : sZ;

            return identifier + "0x" + (sX + sY + sZ).ToUpper();
        }
    }
    public partial class MainWindow : Window
    {
        enum DrawingMode { VECTOR, CIRCLE }

        List<Coordinate> points = new List<Coordinate>();

        public int snapSize = 10;
        private int x, y;
        private bool drawing = false;
        private double mRadius;
        private Line helperVector = new Line();
        private Ellipse helperEllipse = new Ellipse();
        private Ellipse pointerEllipse = new Ellipse();
        private DrawingMode mDrawingMode = DrawingMode.VECTOR; 
        
        public MainWindow()
        {
            InitializeComponent();
            Thread t = new Thread(new ThreadStart(SerialWrite));
            t.Start();
        }

        public void SerialWrite()
        {
            try
            {
                SerialPort serial = new SerialPort("COM8", 9600);
                serial.Open();

                int i = 0;
                int response = serial.ReadByte();
                while (true)
                {
                    while (response != 13 || i == points.Count)
                    {
                        Thread.Sleep(1);
                        response = serial.ReadByte();
                    }
                    string msg = points[i++].ToString();
                    serial.Write(msg);
                }
            } catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mouse = Mouse.GetPosition(canvas);
            Coordinate start = new Coordinate();

            start.x = (int)mouse.X;
            start.y = (int)mouse.Y;

            start.x -= (start.x % snapSize);
            start.y -= (start.y % snapSize);

            x = start.x;
            y = start.y;

            switch (mDrawingMode)
            {
                case DrawingMode.VECTOR:
                    break;
                case DrawingMode.CIRCLE:
                    start.identifier = 'v'; // Vector
                    start.z = 1;
                    points.Add(start);
                    break;
            }
            drawing = true;
        }
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            Point point = Mouse.GetPosition(canvas);
            Coordinate end = new Coordinate();

            end.x = (int)point.X;
            end.y = (int)point.Y;
            
            end.x -= (end.x % snapSize);
            end.y -= (end.y % snapSize);

            switch (mDrawingMode)
            {
                case DrawingMode.VECTOR:
                    end.identifier = 'v'; // Vector
                    end.z = 0;

                    DrawVector(x, y, end.x, end.y);
                    break;
                case DrawingMode.CIRCLE:
                    end.identifier = 'c'; // Circle
                    end.x = x;
                    end.y = y;
                    end.z = Convert.ToInt32(mRadius);

                    DrawEllipse(x, y, end.z);
                    break;
            }

            points.Add(end);
            drawing = false;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point point = Mouse.GetPosition(canvas);

            ClearCanvas();

            int endX = (int)point.X;
            int endY = (int)point.Y;

            endX -= (endX % snapSize);
            endY -= (endY % snapSize);

            if (drawing)
            {
                switch (mDrawingMode)
                {
                    case DrawingMode.VECTOR:
                        helperVector = DrawVector(x, y, endX, endY);
                        break;
                    case DrawingMode.CIRCLE:
                        int xLen = Math.Abs(x - endX);
                        int yLen = Math.Abs(y - endY);

                        mRadius = Math.Sqrt((xLen * xLen) + (yLen * yLen));

                        helperEllipse = DrawEllipse(x, y, Convert.ToInt32(mRadius));
                        break;
                }
            }
            DrawPointerEllipse(endX, endY);
        }

        private void ClearCanvas()
        {
            if (canvas.Children.Contains(helperVector))
            {
                canvas.Children.Remove(helperVector);
            }

            if (canvas.Children.Contains(pointerEllipse))
            {
                canvas.Children.Remove(pointerEllipse);
            }

            if (canvas.Children.Contains(helperEllipse))
            {
                canvas.Children.Remove(helperEllipse);
            }
        }

        private Line DrawVector(int startX, int startY, int endX, int endY)
        {
            Line vector = new Line();

            vector.Stroke = System.Windows.Media.Brushes.AliceBlue;
            vector.X1 = startX;
            vector.Y1 = startY;
            vector.X2 = endX;
            vector.Y2 = endY;
            vector.HorizontalAlignment = HorizontalAlignment.Left;
            vector.VerticalAlignment = VerticalAlignment.Center;
            vector.StrokeThickness = 2;

            canvas.Children.Add(vector);

            return vector;
        }

        private Ellipse DrawEllipse(int x, int y, int radius)
        {
            Ellipse ellipse = new Ellipse();

            ellipse.StrokeThickness = 2;
            ellipse.Stroke = System.Windows.Media.Brushes.AliceBlue;
            ellipse.Width = radius * 2;
            ellipse.Height = radius * 2;
            ellipse.Margin = new Thickness(x - radius, y - radius, 0, 0);

            canvas.Children.Add(ellipse);

            return ellipse;
        }
        
        private void DrawPointerEllipse(int x, int y)
        {
            int radius = 4;
            pointerEllipse.StrokeThickness = 2;
            pointerEllipse.Stroke = System.Windows.Media.Brushes.Black;
            pointerEllipse.Width = radius * 2;
            pointerEllipse.Height = radius * 2;
            pointerEllipse.Margin = new Thickness(x - radius, y - radius, 0, 0);

            canvas.Children.Add(pointerEllipse);
        }
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl)
            {
                mDrawingMode = DrawingMode.CIRCLE;
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl)
            {
                mDrawingMode = DrawingMode.VECTOR;
            }
        }
    }
}
