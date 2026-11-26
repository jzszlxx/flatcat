// FlatCat - 截图：全屏 / 矩形选区 / 不规则套索
// Copyright (c) 2026 认知觉醒能量站 (微信公众号同名)。MIT License，见 LICENSE。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace FlatCat
{
    internal enum ShotMode { FullScreen, Rect, Lasso }

    /// <summary>抓屏、裁剪与落盘。</summary>
    internal static class Shot
    {
        public static Rectangle VirtualBounds
        {
            get { return SystemInformation.VirtualScreen; }
        }

        /// <summary>抓取虚拟屏（可含负坐标）的一块区域。</summary>
        public static Bitmap Grab(Rectangle r)
        {
            int w = Math.Max(1, r.Width), h = Math.Max(1, r.Height);
            Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(r.Left, r.Top, 0, 0, new Size(w, h), CopyPixelOperation.SourceCopy);
            }
            return bmp;
        }

        /// <summary>按多边形裁剪（坐标为屏幕绝对坐标），输出带 alpha 的位图。</summary>
        public static Bitmap CropToPolygon(Bitmap source, Rectangle sourceOrigin, List<Point> screenPoints)
        {
            if (screenPoints == null || screenPoints.Count < 3) return source;

            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            for (int i = 0; i < screenPoints.Count; i++)
            {
                Point p = screenPoints[i];
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }
            Rectangle box = new Rectangle(minX, minY, Math.Max(1, maxX - minX), Math.Max(1, maxY - minY));
            Rectangle local = new Rectangle(box.X - sourceOrigin.X, box.Y - sourceOrigin.Y, box.Width, box.Height);

            Point[] poly = new Point[screenPoints.Count];
            for (int i = 0; i < screenPoints.Count; i++)
                poly[i] = new Point(screenPoints[i].X - box.X, screenPoints[i].Y - box.Y);

            Bitmap output = new Bitmap(box.Width, box.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(output))
            using (GraphicsPath path = new GraphicsPath())
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                path.AddPolygon(poly);
                g.SetClip(path);
                g.DrawImage(source, new Rectangle(0, 0, box.Width, box.Height), local, GraphicsUnit.Pixel);
            }
            return output;
        }

        public static string DefaultDir
        {
            get
            {
                string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                if (string.IsNullOrEmpty(pictures)) pictures = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                return Path.Combine(pictures, "FlatCat 截图");
            }
        }

        /// <summary>保存 PNG，返回路径；失败返回 null。</summary>
        public static string Save(Bitmap bmp, string dir)
        {
            try
            {
                if (string.IsNullOrEmpty(dir)) dir = DefaultDir;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string name = "FlatCat_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
                string path = Path.Combine(dir, name);
                int n = 2;
                while (File.Exists(path))
                {
                    path = Path.Combine(dir, "FlatCat_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + n + ".png");
                    n++;
                }
                bmp.Save(path, ImageFormat.Png);
                return path;
            }
            catch
            {
                return null;
            }
        }

        public static bool CopyToClipboard(Bitmap bmp)
        {
            try
            {
                Clipboard.SetImage(bmp);
                return true;
            }
            catch { return false; }
        }
    }

    /// <summary>覆盖整个虚拟屏的取景遮罩：矩形拖框或套索描边。</summary>
    internal sealed class CaptureOverlay : Form
    {
        private readonly ShotMode _mode;
        private readonly Rectangle _bounds;

        private bool _dragging;
        private Point _start;
        private Point _current;
        private List<Point> _points = new List<Point>();
        private bool _done;

        /// <summary>矩形模式下的选区（屏幕坐标）。</summary>
        public Rectangle SelectedRect;
        /// <summary>套索模式下的多边形顶点（屏幕坐标）。</summary>
        public List<Point> SelectedPoints;

        public CaptureOverlay(ShotMode mode)
        {
            _mode = mode;
            _bounds = Shot.VirtualBounds;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = _bounds;
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = Color.Black;
            Opacity = 0.35;
            DoubleBuffered = true;
            Cursor = Cursors.Cross;
            KeyPreview = true;
            Text = "FlatCat 截图";
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Activate();
            Focus();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) { _done = false; Close(); return; }
            base.OnKeyDown(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) { Close(); return; }
            if (e.Button != MouseButtons.Left) return;

            Point screen = PointToScreen(e.Location);
            if (_mode == ShotMode.Lasso)
            {
                _points = new List<Point>();
                _points.Add(screen);
                _dragging = true;
            }
            else
            {
                _start = screen;
                _current = screen;
                _dragging = true;
            }
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!_dragging) return;
            Point screen = PointToScreen(e.Location);

            if (_mode == ShotMode.Lasso)
            {
                Point last = _points[_points.Count - 1];
                if (Math.Abs(screen.X - last.X) + Math.Abs(screen.Y - last.Y) >= 3)
                    _points.Add(screen);
            }
            else
            {
                _current = screen;
            }
            Invalidate();
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (!_dragging || e.Button != MouseButtons.Left) return;
            _dragging = false;

            if (_mode == ShotMode.Lasso)
            {
                SelectedPoints = new List<Point>(_points);
                if (SelectedPoints.Count >= 3) { _done = true; Close(); }
                else Invalidate();
            }
            else
            {
                Rectangle r = Normalize(_start, _current);
                if (r.Width >= 4 && r.Height >= 4)
                {
                    SelectedRect = r;
                    _done = true;
                    Close();
                }
                else Invalidate();
            }
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 顶部提示
            string tip;
            if (_mode == ShotMode.Lasso) tip = "按住左键沿要截的轮廓描一圈，松手完成 · Esc 取消";
            else tip = "按住左键拖出要截的区域，松手完成 · Esc 取消";
            using (Font f = new Font("Microsoft YaHei", 11f))
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(200, 38, 34, 32)))
            using (SolidBrush fg = new SolidBrush(Color.FromArgb(247, 242, 234)))
            {
                SizeF sz = g.MeasureString(tip, f);
                RectangleF box = new RectangleF((Width - sz.Width) / 2f - 14, 24, sz.Width + 28, sz.Height + 12);
                g.FillRectangle(bg, box);
                g.DrawString(tip, f, fg, box.X + 14, box.Y + 6);
            }

            if (_mode == ShotMode.Lasso)
            {
                if (_points.Count > 1)
                {
                    Point[] pts = new Point[_points.Count];
                    for (int i = 0; i < _points.Count; i++)
                        pts[i] = PointToClient(_points[i]);
                    using (Pen pen = new Pen(Color.FromArgb(196, 86, 63), 2.4f))
                    using (SolidBrush fill = new SolidBrush(Color.FromArgb(60, 255, 255, 255)))
                    {
                        g.FillPolygon(fill, pts);
                        g.DrawLines(pen, pts);
                    }
                }
            }
            else if (_dragging)
            {
                Rectangle r = Normalize(_start, _current);
                Rectangle local = new Rectangle(r.X - _bounds.X, r.Y - _bounds.Y, r.Width, r.Height);
                using (Pen pen = new Pen(Color.FromArgb(196, 86, 63), 2f))
                using (SolidBrush fill = new SolidBrush(Color.FromArgb(56, 255, 255, 255)))
                {
                    g.FillRectangle(fill, local);
                    g.DrawRectangle(pen, local);
                }
                using (Font f = new Font("Microsoft YaHei", 10f))
                using (SolidBrush fg = new SolidBrush(Color.FromArgb(247, 242, 234)))
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(200, 38, 34, 32)))
                {
                    string txt = r.Width + " × " + r.Height;
                    SizeF sz = g.MeasureString(txt, f);
                    float tx = local.Right + 8, ty = local.Bottom + 8;
                    if (tx + sz.Width + 12 > Width) tx = Width - sz.Width - 20;
                    if (ty + sz.Height + 8 > Height) ty = Height - sz.Height - 14;
                    g.FillRectangle(bg, tx - 6, ty - 3, sz.Width + 12, sz.Height + 6);
                    g.DrawString(txt, f, fg, tx, ty);
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (!_done) { SelectedRect = Rectangle.Empty; SelectedPoints = null; }
        }

        private static Rectangle Normalize(Point a, Point b)
        {
            int x = Math.Min(a.X, b.X), y = Math.Min(a.Y, b.Y);
            return new Rectangle(x, y, Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
        }
    }
}