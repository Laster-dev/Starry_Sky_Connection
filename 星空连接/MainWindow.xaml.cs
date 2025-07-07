using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace 星空连接
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private class Star
        {
            public double X, Y, R;
            public Color Color;
            public double SpeedX, SpeedY;
        }

        private List<Star> stars = new List<Star>(); // 存储星星信息
        private Random rand = new Random(); // 随机数生成器
        private Point mousePos = new Point(-1000, -1000); // 鼠标位置，初始值为屏幕外
        private const int StarCount = 150; // 星星的数量
        private const double StarMinR = 1, StarMaxR = 3; // 星星的最小和最大半径
        private const double LineDistance = 80; // 星星之间连线的最大距离
        private const double MouseLineDistance = 120; // 鼠标与星星连线的最大距离
        private const double MouseAttractionForce = 0.03; // 鼠标吸引力强度，可调整
        private const double MouseAttractionRange = 50; // 鼠标吸引力作用范围

        public MainWindow()
        {
            InitializeComponent();
        }

        private void StarCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            stars.Clear();
            double w = StarCanvas.ActualWidth;
            double h = StarCanvas.ActualHeight;
            for (int i = 0; i < StarCount; i++)
                stars.Add(CreateStar(w, h));

            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(16);
            timer.Tick += (s, args) => Draw();
            timer.Start();
        }

        private Star CreateStar(double w, double h)
        {
            return new Star
            {
                X = rand.NextDouble() * (w - 6) + 3,
                Y = rand.NextDouble() * (h - 6) + 3,
                R = rand.NextDouble() * (StarMaxR - StarMinR) + StarMinR,
                Color = Color.FromRgb((byte)rand.Next(256), (byte)rand.Next(256), (byte)rand.Next(256)),
                SpeedX = (rand.NextDouble() * 4 - 2) * 0.2,
                SpeedY = (rand.NextDouble() * 6 - 3) * 0.2
            };
        }

        private void Draw()
        {
            double w = StarCanvas.ActualWidth;
            double h = StarCanvas.ActualHeight;
            if (w == 0 || h == 0) return;

            StarCanvas.Children.Clear();

            // 星星移动与绘制
            foreach (var star in stars)
            {
                // 基础移动
                star.X += star.SpeedX;
                star.Y += star.SpeedY;
                
                // 鼠标吸引力效果
                double dx = mousePos.X - star.X;
                double dy = mousePos.Y - star.Y;
                double distToMouse = Math.Sqrt(dx * dx + dy * dy);
                
                if (distToMouse < MouseAttractionRange && distToMouse > 0)
                {
                    // 计算吸引力，距离越近吸引力越强
                    double attractionStrength = MouseAttractionForce * (1.0 - distToMouse / MouseAttractionRange);
                    star.SpeedX += (dx / distToMouse) * attractionStrength;
                    star.SpeedY += (dy / distToMouse) * attractionStrength;
                }
                
                // 边界反弹
                if (star.X <= 3 || star.X >= w - 3) star.SpeedX *= -1;
                if (star.Y <= 3 || star.Y >= h - 3) star.SpeedY *= -1;

                var ellipse = new Ellipse
                {
                    Width = star.R * 2,
                    Height = star.R * 2,
                    Fill = new SolidColorBrush(star.Color),
                    Opacity = 1
                };
                Canvas.SetLeft(ellipse, star.X - star.R);
                Canvas.SetTop(ellipse, star.Y - star.R);
                StarCanvas.Children.Add(ellipse);
            }

            // 星星之间连线
            for (int i = 0; i < stars.Count; i++)
            {
                for (int j = i + 1; j < stars.Count; j++)
                {
                    double dx = stars[i].X - stars[j].X;
                    double dy = stars[i].Y - stars[j].Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist < LineDistance)
                    {
                        double opacity = 1.0 - (dist / LineDistance);
                        var line = new Line
                        {
                            X1 = stars[i].X,
                            Y1 = stars[i].Y,
                            X2 = stars[j].X,
                            Y2 = stars[j].Y,
                            Stroke = Brushes.White,
                            Opacity = 0.2 * opacity + 0.05,
                            StrokeThickness = 1
                        };
                        StarCanvas.Children.Add(line);
                    }
                }
            }

            // 鼠标与星星连线
            foreach (var star in stars)
            {
                double dx = star.X - mousePos.X;
                double dy = star.Y - mousePos.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < MouseLineDistance)
                {
                    double opacity = 1.0 - (dist / MouseLineDistance);
                    var line = new Line
                    {
                        X1 = star.X,
                        Y1 = star.Y,
                        X2 = mousePos.X,
                        Y2 = mousePos.Y,
                        Stroke = Brushes.White,
                        Opacity = 0.8 * opacity + 0.1,
                        StrokeThickness = 1
                    };
                    StarCanvas.Children.Add(line);
                }
            }
        }

        private void StarCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            mousePos = e.GetPosition(StarCanvas);
        }
    }
}
