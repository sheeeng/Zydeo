﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;


namespace DND.Controls
{
    public class ZenTabbedForm : Form, IZenControlOwner
    {
        private readonly int headerHeight;
        private readonly int innerPadding;
        private readonly float scale;
        private Bitmap dbuffer = null;
        private Panel contentPanel;
        private readonly List<ZenControl> controls = new List<ZenControl>();
        private ZenCloseControl ctrlClose;
        private bool controlsCreated = false;

        public ZenTabbedForm()
        {
            SuspendLayout();

            DoubleBuffered = false;
            FormBorderStyle = FormBorderStyle.None;

            Size = new Size(800, 300);
            contentPanel = new Panel();
            contentPanel.BackColor = ZenParams.PaddingBackColor;
            contentPanel.BorderStyle = BorderStyle.None;
            contentPanel.Location = new Point((int)ZenParams.InnerPadding, (int)ZenParams.HeaderHeight);
            contentPanel.Size = new Size(
                Width - 2 * (int)ZenParams.InnerPadding,
                Height - (int)ZenParams.InnerPadding - (int)ZenParams.HeaderHeight);
            Controls.Add(contentPanel);
            AutoScaleDimensions = new SizeF(6.0F, 13.0F);
            AutoScaleMode = AutoScaleMode.Font;
            scale = CurrentAutoScaleDimensions.Height / 13.0F;
            ResumeLayout();

            headerHeight = (int)(ZenParams.HeaderHeight * scale);
            innerPadding = (int)(ZenParams.InnerPadding * scale);

            doCreateControls();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x20000;
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (dbuffer != null) { dbuffer.Dispose(); dbuffer = null; }
            }
        }

        private void addControl(ZenControl ctrl)
        {
            controls.Add(ctrl);
            ctrl.Owner = this;
        }

        public void SetControl(Control c)
        {
            c.Dock = DockStyle.Fill;
            c.Location = new Point(0, 0);
            c.Size = contentPanel.ClientSize;
            contentPanel.Controls.Add(c);
        }

        private void doCreateControls()
        {
            ctrlClose = new ZenCloseControl(scale);
            ctrlClose.LogicalSize = new Size(40, 20);
            ctrlClose.Location = new Point(Width - ctrlClose.Size.Width - innerPadding, 0);
            ctrlClose.MouseClick += ctrlClose_MouseClick;
            addControl(ctrlClose);

            controlsCreated = true;
        }

        private void arrangeControls()
        {
            if (!controlsCreated) return;
            
            contentPanel.Location = new Point(innerPadding, headerHeight);
            contentPanel.Size = new Size(
                Width - 2 * innerPadding,
                Height - innerPadding - headerHeight);


            ctrlClose.Location = new Point(Width - ctrlClose.Size.Width - innerPadding, 0);
        }

        void ctrlClose_MouseClick(ZenControl sender)
        {
            Close();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            if (dbuffer != null)
            {
                dbuffer.Dispose();
                dbuffer = null;
            }
            base.OnSizeChanged(e);
            arrangeControls();
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // NOP!
        }

        private void doPaintMyBackground(Graphics g)
        {
            using (Brush b = new SolidBrush(ZenParams.HeaderBackColor))
            {
                g.FillRectangle(b, 0, 0, Width, headerHeight);
            }
            using (Brush b = new SolidBrush(ZenParams.PaddingBackColor))
            {
                g.FillRectangle(b, 0, headerHeight, innerPadding, Height - headerHeight);
                g.FillRectangle(b, Width - innerPadding, headerHeight, innerPadding, Height - headerHeight);
                g.FillRectangle(b, innerPadding, Height - innerPadding, Width - 2 * innerPadding, innerPadding);
            }
            using (Pen p = new Pen(ZenParams.BorderColor))
            {
                p.Width = 1;
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            }
        }

        public void Invalidate(ZenControl ctrl)
        {
            // TO-DO: optimize: trigger paint that only paints affected control
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics gg = e.Graphics;
            // Do all the remaining drawing through my own hand-made double-buffering for speed
            if (dbuffer == null)
                dbuffer = new Bitmap(Width, Height);
            using (Graphics g = Graphics.FromImage(dbuffer))
            {
                doPaintMyBackground(g);
                foreach (ZenControl ctrl in controls) ctrl.DoPaint(g);
            }
            e.Graphics.DrawImageUnscaled(dbuffer, 0, 0);
        }

        private enum DragMode
        {
            None,
            ResizeW,
            ResizeE,
            ResizeN,
            ResizeS,
            ResizeNW,
            ResizeSE,
            ResizeNE,
            ResizeSW,
            Move
        }

        private DragMode dragMode = DragMode.None;
        private Point dragStart;
        private Point formBeforeDragLocation;
        private Size formBeforeDragSize;

        private DragMode getDragArea(Point p)
        {
            Rectangle rHeader = new Rectangle(
                innerPadding,
                innerPadding,
                Width - 2 * innerPadding,
                headerHeight - innerPadding);
            if (rHeader.Contains(p))
                return DragMode.Move;
            Rectangle rEast = new Rectangle(
                Width - innerPadding,
                0,
                innerPadding,
                Height);
            if (rEast.Contains(p))
            {
                if (p.Y < 2 * innerPadding) return DragMode.ResizeNE;
                if (p.Y > Height - 2 * innerPadding) return DragMode.ResizeSE;
                return DragMode.ResizeE;
            }
            Rectangle rWest = new Rectangle(
                0,
                0,
                innerPadding,
                Height);
            if (rWest.Contains(p))
            {
                if (p.Y < 2 * innerPadding) return DragMode.ResizeNW;
                if (p.Y > Height - 2 * innerPadding) return DragMode.ResizeSW;
                return DragMode.ResizeW;
            }
            Rectangle rNorth = new Rectangle(
                0,
                0,
                Width,
                innerPadding);
            if (rNorth.Contains(p))
            {
                if (p.X < 2 * innerPadding) return DragMode.ResizeNW;
                if (p.X > Width - 2 * innerPadding) return DragMode.ResizeNE;
                return DragMode.ResizeN;
            }
            Rectangle rSouth = new Rectangle(
                0,
                Height - innerPadding,
                Width,
                innerPadding);
            if (rSouth.Contains(p))
            {
                if (p.X < 2 * innerPadding) return DragMode.ResizeSW;
                if (p.X > Width - 2 * innerPadding) return DragMode.ResizeSE;
                return DragMode.ResizeS;
            }
            return DragMode.None;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            // Over any control? Not our job to handle.
            ZenControl ctrl = getControl(e.Location);
            if (ctrl != null)
            {
                ctrl.DoMouseDown(translateToControl(ctrl, e.Location), e.Button);
                return;
            }

            var area = getDragArea(e.Location);
            if (area == DragMode.Move)
            {
                dragMode = DragMode.Move;
                dragStart = PointToScreen(e.Location);
                formBeforeDragLocation = Location;
            }
            else if (area != DragMode.None && area != DragMode.Move)
            {
                dragMode = area;
                dragStart = PointToScreen(e.Location);
                formBeforeDragSize = Size;
                formBeforeDragLocation = Location;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            Point loc = PointToScreen(e.Location);
            if (dragMode == DragMode.Move)
            {
                int dx = loc.X - dragStart.X;
                int dy = loc.Y - dragStart.Y;
                Point newLocation = new Point(formBeforeDragLocation.X + dx, formBeforeDragLocation.Y + dy);
                ((Form)TopLevelControl).Location = newLocation;
                return;
            }
            else if (dragMode == DragMode.ResizeE)
            {
                int dx = loc.X - dragStart.X;
                Size = new Size(formBeforeDragSize.Width + dx, formBeforeDragSize.Height);
                return;
            }
            else if (dragMode == DragMode.ResizeW)
            {
                int dx = loc.X - dragStart.X;
                Left = formBeforeDragLocation.X + dx;
                Size = new Size(formBeforeDragSize.Width - dx, formBeforeDragSize.Height);
                return;
            }
            else if (dragMode == DragMode.ResizeN)
            {
                int dy = loc.Y - dragStart.Y;
                Top = formBeforeDragLocation.Y + dy;
                Size = new Size(formBeforeDragSize.Width, formBeforeDragSize.Height - dy);
                return;
            }
            else if (dragMode == DragMode.ResizeS)
            {
                int dy = loc.Y - dragStart.Y;
                Size = new Size(formBeforeDragSize.Width, formBeforeDragSize.Height + dy);
                return;
            }
            else if (dragMode == DragMode.ResizeNW)
            {
                int dx = loc.X - dragStart.X;
                int dy = loc.Y - dragStart.Y;
                Size = new Size(formBeforeDragSize.Width - dx, formBeforeDragSize.Height - dy);
                Location = new Point(formBeforeDragLocation.X + dx, formBeforeDragLocation.Y + dy);
                return;
            }
            else if (dragMode == DragMode.ResizeSE)
            {
                int dx = loc.X - dragStart.X;
                int dy = loc.Y - dragStart.Y;
                Size = new Size(formBeforeDragSize.Width + dx, formBeforeDragSize.Height + dy);
                return;
            }
            else if (dragMode == DragMode.ResizeNE)
            {
                int dx = loc.X - dragStart.X;
                int dy = loc.Y - dragStart.Y;
                Size = new Size(formBeforeDragSize.Width + dx, formBeforeDragSize.Height - dy);
                Location = new Point(formBeforeDragLocation.X, formBeforeDragLocation.Y + dy);
                return;
            }
            else if (dragMode == DragMode.ResizeSW)
            {
                int dx = loc.X - dragStart.X;
                int dy = loc.Y - dragStart.Y;
                Size = new Size(formBeforeDragSize.Width - dx, formBeforeDragSize.Height + dy);
                Location = new Point(formBeforeDragLocation.X + dx, formBeforeDragLocation.Y);
                return;
            }

            // Over any controls? Not our job to handle.
            ZenControl ctrl = getControl(e.Location);
            if (ctrl != null)
            {
                Cursor = Cursors.Arrow;
                ctrl.DoMouseMove(translateToControl(ctrl, e.Location), e.Button);
                return;
            }

            var area = getDragArea(e.Location);
            if (area == DragMode.ResizeW || area == DragMode.ResizeE)
                Cursor = Cursors.SizeWE;
            else if (area == DragMode.ResizeN || area == DragMode.ResizeS)
                Cursor = Cursors.SizeNS;
            else if (area == DragMode.ResizeNW || area == DragMode.ResizeSE)
                Cursor = Cursors.SizeNWSE;
            else if (area == DragMode.ResizeNE || area == DragMode.ResizeSW)
                Cursor = Cursors.SizeNESW;
            else Cursor = Cursors.Arrow;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (dragMode == DragMode.None || dragMode == DragMode.Move)
            {
                Cursor = Cursors.Arrow;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            dragMode = DragMode.None;

            ZenControl ctrl = getControl(e.Location);
            if (ctrl != null) ctrl.DoMouseUp(translateToControl(ctrl, e.Location), e.Button);
        }

        private ZenControl getControl(Point p)
        {
            foreach (ZenControl ctrl in controls)
                if (ctrl.Contains(p)) return ctrl;
            return null;
        }

        private Point translateToControl(ZenControl ctrl, Point p)
        {
            int x = p.X - ctrl.Location.X;
            int y = p.Y - ctrl.Location.Y;
            return new Point(x, y);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            ZenControl ctrl = getControl(e.Location);
            if (ctrl != null) ctrl.DoMouseClick(translateToControl(ctrl, e.Location), e.Button);
        }
    }
}
