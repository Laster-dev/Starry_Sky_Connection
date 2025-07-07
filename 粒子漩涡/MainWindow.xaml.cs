using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Color = System.Drawing.Color;

namespace 粒子漩涡
{
    public partial class MainWindow : Window
    {
        class Particle
        {
            public float x, y, z, vy, radius, color, dist;
        }

        float camX = 0, camY = 0, camZ = -14;
        float pitch, yaw;
        float scale = 500, floor = 26.5f;
        int frameNo = 0;
        int initParticles = 250;
        float initV = 0.01f;
        float distributionRadius = 800;
        float vortexHeight = 25;
        List<Particle> points = new List<Particle>();
        Random rand = new Random();

        Bitmap bmp;
        WriteableBitmap wb;
        int width = 500, height = 300;
        float cx, cy;
        DispatcherTimer timer;
        Graphics g;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            SizeChanged += MainWindow_SizeChanged;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitBitmap();
            StartAnimation();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            InitBitmap();
        }

        private void InitBitmap()
        {
            width = (int)Math.Max(1, MainImage.ActualWidth / 2);
            height = (int)Math.Max(1, MainImage.ActualHeight / 2);
            cx = width / 2f;
            cy = height / 2f;
            if (bmp != null) bmp.Dispose();
            bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            if (wb == null || wb.PixelWidth != width || wb.PixelHeight != height)
                wb = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
            MainImage.Source = wb;
        }

        private void StartAnimation()
        {
            if (timer != null) timer.Stop();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(16);
            timer.Tick += (s, e) => Frame();
            timer.Start();
        }

        private (float x, float y, float d) Project3D(float x, float y, float z)
        {
            x -= camX;
            y -= camY - 8;
            z -= camZ;
            float p = (float)Math.Atan2(x, z);
            float d = (float)Math.Sqrt(x * x + z * z);
            x = (float)(Math.Sin(p - yaw) * d);
            z = (float)(Math.Cos(p - yaw) * d);
            p = (float)Math.Atan2(y, z);
            d = (float)Math.Sqrt(y * y + z * z);
            y = (float)(Math.Sin(p - pitch) * d);
            z = (float)(Math.Cos(p - pitch) * d);
            if (z == 0) z = 0.000000001f;
            float px = cx + x / z * scale;
            float py = cy + y / z * scale;
            float dist = x * x + y * y + z * z;
            return (px, py, dist);
        }

        private float Elevation(float x, float y, float z)
        {
            float dist = (float)Math.Sqrt(x * x + y * y + z * z);
            if (dist != 0 && z / dist >= -1 && z / dist <= 1)
                return (float)Math.Acos(z / dist);
            return 0.00000001f;
        }

        private Color Rgb(float col)
        {
            col += 0.000001f;
            byte r = (byte)((0.5 + Math.Sin(col) * 0.5) * 255);
            byte g = (byte)((0.5 + Math.Cos(col) * 0.5) * 255);
            byte b = (byte)((0.5 - Math.Sin(col) * 0.5) * 255);
            return Color.FromArgb(255, r, g, b);
        }

        private Color InterpolateColors(Color c1, Color c2, float degree)
        {
            float w2 = degree;
            float w1 = 1 - w2;
            return Color.FromArgb(
                255,
                (byte)(w1 * c1.R + w2 * c2.R),
                (byte)(w1 * c1.G + w2 * c2.G),
                (byte)(w1 * c1.B + w2 * c2.B)
            );
        }

        private Color RgbArray(float col)
        {
            col += 0.000001f;
            byte r = (byte)((0.5 + Math.Sin(col) * 0.5) * 255);
            byte g = (byte)((0.5 + Math.Cos(col) * 0.5) * 255);
            byte b = (byte)((0.5 - Math.Sin(col) * 0.5) * 255);
            return Color.FromArgb(255, r, g, b);
        }

        private void SpawnParticle()
        {
            float p = (float)(2 * Math.PI * rand.NextDouble());
            float ls = (float)Math.Sqrt(rand.NextDouble() * distributionRadius);
            var pt = new Particle
            {
                x = (float)(Math.Sin(p) * ls),
                y = -vortexHeight / 2,
                vy = initV / 20 + (float)rand.NextDouble() * initV,
                z = (float)(Math.Cos(p) * ls),
                radius = 200 + 800 * (float)rand.NextDouble(),
                color = (200 + 800 * (float)rand.NextDouble()) / 1000 + frameNo / 250.0f
            };
            points.Add(pt);
        }

        private void Process()
        {
            if (points.Count < initParticles)
                for (int i = 0; i < 5; ++i) SpawnParticle();

            float p = (float)Math.Atan2(camX, camZ);
            float d = (float)Math.Sqrt(camX * camX + camZ * camZ);
            d -= (float)Math.Sin(frameNo / 80.0) / 25.0f;
            float t = (float)Math.Cos(frameNo / 300.0) / 165.0f;
            camX = (float)(Math.Sin(p + t) * d);
            camZ = (float)(Math.Cos(p + t) * d);
            camY = -(float)Math.Sin(frameNo / 220.0) * 15.0f;
            yaw = (float)(Math.PI + p + t);
            pitch = Elevation(camX, camZ, camY) - (float)Math.PI / 2;

            for (int i = points.Count - 1; i >= 0; i--)
            {
                var pt = points[i];
                float x = pt.x, y = pt.y, z = pt.z;
                float d2 = (float)Math.Sqrt(x * x + z * z) / 1.0075f;
                float t2 = 0.1f / (1 + d2 * d2 / 5);
                float p2 = (float)Math.Atan2(x, z) + t2;
                pt.x = (float)(Math.Sin(p2) * d2);
                pt.z = (float)(Math.Cos(p2) * d2);
                pt.y += pt.vy * t2 * ((float)Math.Sqrt(distributionRadius) - d2) * 2;
                if (pt.y > vortexHeight / 2 || d2 < 0.25)
                {
                    points.RemoveAt(i);
                    SpawnParticle();
                }
            }
            frameNo++;
        }

        private void Frame()
        {
            Process();

            if (g == null || g.VisibleClipBounds.Width != width || g.VisibleClipBounds.Height != height)
            {
                if (g != null) g.Dispose();
                g = Graphics.FromImage(bmp);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            }

            // 拖尾效果
            using (SolidBrush sb = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
            {
                g.FillRectangle(sb, 0, 0, width, height);
            }

            DrawFloor(g);
            DrawParticles(g);

            // 拷贝到 WriteableBitmap
            var rect = new Int32Rect(0, 0, width, height);
            var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            wb.WritePixels(rect, data.Scan0, data.Stride * height, data.Stride);
            bmp.UnlockBits(data);
        }

        private void DrawFloor(Graphics g)
        {
            for (int i = -15; i <= 15; i++)
            {
                for (int j = -15; j <= 15; j++)
                {
                    float x = i * 2;
                    float z = j * 2;
                    float y = floor;
                    float d = (float)Math.Sqrt(x * x + z * z);
                    var (px, py, dist) = Project3D(x, y - d * d / 85, z);
                    float size = 1 + 15000 / (1 + dist);
                    float a = 0.15f - (float)Math.Pow(d / 50, 4) * 0.15f;
                    if (a > 0)
                    {
                        var c1 = RgbArray(d / 26 - frameNo / 40.0f);
                        var c2 = Color.FromArgb(255, 0, 128, 32);
                        var color = InterpolateColors(c1, c2, 0.5f + (float)Math.Sin(d / 6 - frameNo / 8.0f) / 2);
                        using (SolidBrush sb = new SolidBrush(Color.FromArgb((int)(a * 255), color)))
                        {
                            g.FillRectangle(sb, px - size / 2, py - size / 2, size, size);
                        }
                    }
                }
            }
            for (int i = -15; i <= 15; i++)
            {
                for (int j = -15; j <= 15; j++)
                {
                    float x = i * 2;
                    float z = j * 2;
                    float y = -floor;
                    float d = (float)Math.Sqrt(x * x + z * z);
                    var (px, py, dist) = Project3D(x, y + d * d / 85, z);
                    float size = 1 + 15000 / (1 + dist);
                    float a = 0.15f - (float)Math.Pow(d / 50, 4) * 0.15f;
                    if (a > 0)
                    {
                        var c1 = RgbArray(-d / 26 - frameNo / 40.0f);
                        var c2 = Color.FromArgb(255, 32, 0, 128);
                        var color = InterpolateColors(c1, c2, 0.5f + (float)Math.Sin(-d / 6 - frameNo / 8.0f) / 2);
                        using (SolidBrush sb = new SolidBrush(Color.FromArgb((int)(a * 255), color)))
                        {
                            g.FillRectangle(sb, px - size / 2, py - size / 2, size, size);
                        }
                    }
                }
            }
        }

        private void DrawParticles(Graphics g)
        {
            foreach (var pt in points)
            {
                var (px, py, dist) = Project3D(pt.x, pt.y, pt.z);
                pt.dist = dist;
            }
            points.Sort((a, b) => b.dist.CompareTo(a.dist));
            foreach (var pt in points)
            {
                var (px, py, dist) = Project3D(pt.x, pt.y, pt.z);
                float size = 1 + pt.radius / (1 + dist);
                float d = Math.Abs(pt.y);
                float a = 0.8f - (float)Math.Pow(d / (vortexHeight / 2), 1000) * 0.8f;
                if (a < 0) a = 0;
                var color = Rgb(pt.color);
                using (SolidBrush sb = new SolidBrush(Color.FromArgb((int)(a * 255), color)))
                {
                    g.FillRectangle(sb, px - size / 2, py - size / 2, size, size);
                }
            }
        }
    }
}