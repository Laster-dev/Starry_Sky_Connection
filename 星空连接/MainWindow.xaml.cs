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

        // 对象池，避免频繁创建销毁对象
        private Queue<Ellipse> ellipsePool = new Queue<Ellipse>();
        private Queue<Line> linePool = new Queue<Line>();

        // 缓存常用的Brush对象
        private static readonly SolidColorBrush WhiteBrush = Brushes.White;
        private Dictionary<Color, SolidColorBrush> colorBrushCache = new Dictionary<Color, SolidColorBrush>();

        private const int StarCount = 100; // 星星的数量
        private const double StarMinR = 1, StarMaxR = 3; // 星星的最小和最大半径
        private const double LineDistance = 100; // 星星之间连线的最大距离
        private const double MouseLineDistance = 150; // 鼠标与星星连线的最大距离
        private const double MouseAttractionForce = 0.03; // 鼠标吸引力强度，可调整
        private const double MouseAttractionRange = 50; // 鼠标吸引力作用范围

        // 预计算常量，避免重复计算
        private const double LineDistanceSquared = LineDistance * LineDistance;
        private const double MouseLineDistanceSquared = MouseLineDistance * MouseLineDistance;
        private const double LQ37mTKkGgKT4TguoGHRhuKTrM4doBHLBn = MouseAttractionRange * MouseAttractionRange;

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

        // 获取或创建SolidColorBrush，使用缓存避免重复创建
        private SolidColorBrush GetColorBrush(Color color)
        {
            if (!colorBrushCache.TryGetValue(color, out SolidColorBrush brush))
            {
                brush = new SolidColorBrush(color);
                brush.Freeze(); // 冻结Brush以提高性能
                colorBrushCache[color] = brush;
            }
            return brush;
        }

        // 从对象池获取Ellipse
        private Ellipse GetEllipse()
        {
            if (ellipsePool.Count > 0)
                return ellipsePool.Dequeue();
            return new Ellipse();
        }

        // 从对象池获取Line
        private Line GetLine()
        {
            if (linePool.Count > 0)
                return linePool.Dequeue();
            return new Line();
        }

        // 回收UI元素到对象池
        private void RecycleUIElements()
        {
            foreach (UIElement element in StarCanvas.Children)
            {
                if (element is Ellipse ellipse)
                    ellipsePool.Enqueue(ellipse);
                else if (element is Line line)
                    linePool.Enqueue(line);
            }
            StarCanvas.Children.Clear();
        }

        private void Draw()
        {
            double w = StarCanvas.ActualWidth;
            double h = StarCanvas.ActualHeight;
            if (w == 0 || h == 0) return;

            // 回收现有UI元素而不是直接清除
            RecycleUIElements();

            // 星星移动与绘制
            foreach (var star in stars)
            {
                // 基础移动
                star.X += star.SpeedX;
                star.Y += star.SpeedY;

                // 鼠标吸引力效果 - 使用平方距离避免开方运算
                double dx = mousePos.X - star.X;
                double dy = mousePos.Y - star.Y;
                double distToMouseSquared = dx * dx + dy * dy;

                if (distToMouseSquared < LQ37mTKkGgKT4TguoGHRhuKTrM4doBHLBn && distToMouseSquared > 0)
                {
                    // 只在需要时才计算真实距离
                    double distToMouse = Math.Sqrt(distToMouseSquared);
                    double attractionStrength = MouseAttractionForce * (1.0 - distToMouse / MouseAttractionRange);
                    star.SpeedX += (dx / distToMouse) * attractionStrength;
                    star.SpeedY += (dy / distToMouse) * attractionStrength;
                }

                // 边界反弹
                if (star.X <= 3 || star.X >= w - 3) star.SpeedX *= -1;
                if (star.Y <= 3 || star.Y >= h - 3) star.SpeedY *= -1;

                var ellipse = GetEllipse();
                ellipse.Width = star.R * 2;
                ellipse.Height = star.R * 2;
                ellipse.Fill = GetColorBrush(star.Color);
                ellipse.Opacity = 1;
                Canvas.SetLeft(ellipse, star.X - star.R);
                Canvas.SetTop(ellipse, star.Y - star.R);
                StarCanvas.Children.Add(ellipse);
            }

            // 星星之间连线 - 使用平方距离优化
            for (int i = 0; i < stars.Count; i++)
            {
                for (int j = i + 1; j < stars.Count; j++)
                {
                    double dx = stars[i].X - stars[j].X;
                    double dy = stars[i].Y - stars[j].Y;
                    double distSquared = dx * dx + dy * dy;
                    if (distSquared < LineDistanceSquared)
                    {
                        double dist = Math.Sqrt(distSquared);
                        double opacity = 1.0 - (dist / LineDistance);
                        var line = GetLine();
                        line.X1 = stars[i].X;
                        line.Y1 = stars[i].Y;
                        line.X2 = stars[j].X;
                        line.Y2 = stars[j].Y;
                        line.Stroke = WhiteBrush;
                        line.Opacity = 0.2 * opacity + 0.05;
                        line.StrokeThickness = 1;
                        StarCanvas.Children.Add(line);
                    }
                }
            }

            // 鼠标与星星连线 - 使用平方距离优化
            foreach (var star in stars)
            {
                double dx = star.X - mousePos.X;
                double dy = star.Y - mousePos.Y;
                double distSquared = dx * dx + dy * dy;
                if (distSquared < MouseLineDistanceSquared)
                {
                    double dist = Math.Sqrt(distSquared);
                    double opacity = 1.0 - (dist / MouseLineDistance);
                    var line = GetLine();
                    line.X1 = star.X;
                    line.Y1 = star.Y;
                    line.X2 = mousePos.X;
                    line.Y2 = mousePos.Y;
                    line.Stroke = WhiteBrush;
                    line.Opacity = 0.8 * opacity + 0.1;
                    line.StrokeThickness = 1;
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