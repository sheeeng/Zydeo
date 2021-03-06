﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Drawing;
using System.Windows.Forms;

using ZD.Common;
using ZD.Gui.Zen;

namespace ZD.Gui
{
    /// <summary>
    /// Represents the text input field above the results list.
    /// </summary>
    internal class SearchInputControl : ZenControl
    {
        /// <summary>
        /// Delegate for the <see cref="StartSearch"/> event.
        /// </summary>
        public delegate void StartSearchDelegate(object sender, string text);
        /// <summary>
        /// Fired when any user input must trigger a search (e.g., pressing Enter).
        /// </summary>
        public event StartSearchDelegate StartSearch;

        /// <summary>
        /// Localized UI strings provider.
        /// </summary>
        private readonly ITextProvider tprov;
        /// <summary>
        /// Padding inside at my current scale.
        /// </summary>
        private readonly int padding;
        /// <summary>
        /// The hinted text box for entering queries.
        /// </summary>
        private readonly HintedTextBox txtInput;
        /// <summary>
        /// The search button.
        /// </summary>
        private readonly ZenImageButton btnSearch;
        /// <summary>
        /// The "clear text" button.
        /// </summary>
        private readonly ZenImageButton btnCancel;
        /// <summary>
        /// Blocks "size changed" event handler when control changes its own size.
        /// </summary>
        private bool blockSizeChanged = false;

        /// <summary>
        /// Ctor: take parent etc.
        /// </summary>
        public SearchInputControl(ZenControl owner, ITextProvider tprov)
            : base(owner)
        {
            this.tprov = tprov;
            padding = (int)Math.Round(4.0F * Scale);

            // The hinted text input control.
            txtInput = new HintedTextBox();
            txtInput.Name = "txtInput";
            txtInput.TabIndex = 0;
            txtInput.BorderStyle = BorderStyle.None;
            RegisterWinFormsControl(txtInput);
            // My font family, and other properties to achieve a borderless inside input field.
            txtInput.Font = (SystemFontProvider.Instance as ZydeoSystemFontProvider).GetZhoButtonFont(
                FontStyle.Regular, 16F);
            txtInput.AutoSize = false;
            txtInput.Height = txtInput.PreferredHeight + padding;
            txtInput.HintText = tprov.GetString("SearchTextHint");

            // My height depends on text box's height at current font settings.
            blockSizeChanged = true;
            Height = 2 + txtInput.Height;
            blockSizeChanged = false;
            txtInput.KeyPress += onTextBoxKeyPress;

            // Search button
            Assembly a = Assembly.GetExecutingAssembly();
            var imgSearch = Image.FromStream(a.GetManifestResourceStream("ZD.Gui.Resources.search.png"));
            btnSearch = new ZenImageButton(this);
            btnSearch.RelLocation = new Point(padding, padding);
            btnSearch.Size = new Size(Height - 2 * padding, Height - 2 * padding);
            btnSearch.Image = imgSearch;
            btnSearch.MouseClick += onClickSearch;
            
            // Clear text button.
            var imgCancel = Image.FromStream(a.GetManifestResourceStream("ZD.Gui.Resources.cancel.png"));
            btnCancel = new ZenImageButton(this);
            btnCancel.Size = new Size(Height - 2 * padding, Height - 2 * padding);
            btnCancel.Padding = padding; // We want the X to be somewhat smaller
            btnCancel.RelLocation = new Point(Width - padding - btnCancel.Width, padding);
            btnCancel.Image = imgCancel;
            btnCancel.Visible = false;
            btnCancel.MouseClick += onClickCancel;

            txtInput.MouseEnter += onTxtMouseEnter;
            txtInput.MouseLeave += onTxtMouseLeave;
            txtInput.MouseMove += onTxtMouseMove;
            txtInput.TextChanged += onTxtTextChanged;
        }

        /// <summary>
        /// Size changed event handler.
        /// </summary>
        protected override void OnSizeChanged()
        {
            if (blockSizeChanged) return;

            // The height of the text box and icons
            int ctrlHeight = Height - 2 * padding;
            // Position and height below is suitable for Noto, but not for Segoe.
            //int textBoxHeight = Height - 2 * padding;
            //int textBoxTop = AbsTop + padding;
            int textBoxHeight = Height - padding;
            int textBoxTop = AbsTop + (int)(2.5F * Scale);
            // Text field: search icon on left, X icon on right
            // ctrlHeight stands for width of buttons (they're all rectangular)
            // Position must be in absolute (canvas) position, winforms controls' onwer is borderless form.
            // Different for Segoe and Noto to make it look good.
            Point locTxtInput = new Point(AbsLeft + padding, textBoxTop);
            if (txtInput.InvokeRequired)
            {
                InvokeOnForm((MethodInvoker)delegate
                {
                    txtInput.Location = locTxtInput;
                    txtInput.Size = new Size(Width - 4 * padding - 2 * ctrlHeight, textBoxHeight);
                });
            }
            else
            {
                txtInput.Location = locTxtInput;
                txtInput.Size = new Size(Width - 4 * padding - 2 * ctrlHeight, textBoxHeight);
            }
            
            // Search button: right-aligned
            btnSearch.RelLocation = new Point(Width - padding - btnSearch.Width, padding);
            // Cancel button: right-aligned, to the left of search button
            btnCancel.RelLocation = new Point(Width - btnSearch.Width - padding - btnCancel.Width, padding);
        }

        /// <summary>
        /// Insert a character, replacing current selection. This is used when character comes from writing pad.
        /// </summary>
        public void InsertCharacter(char c)
        {
            string str = ""; str += c;
            txtInput.SelectedText = str;
        }

        /// <summary>
        /// Select all text in input field.
        /// </summary>
        public void SelectAll()
        {
            txtInput.SelectAll();
        }

        /// <summary>
        /// Gets or sets the text in the input field.
        /// </summary>
        public string Text
        {
            get { return txtInput.Text; }
            set { txtInput.Text = value; }
        }

        /// <summary>
        /// Triggers the <see cref="StartSearch"/> even if there are any subscribers.
        /// </summary>
        private void doStartSearch()
        {
            if (StartSearch != null)
                StartSearch(this, txtInput.Text);
        }

        /// <summary>
        /// Decides if the "clear text" button should be visible, based on mouse position and text in input field.
        /// </summary>
        /// <returns></returns>
        private bool isCancelVisible()
        {
            if (txtInput.Text == string.Empty) return false;
            Point p = MousePosition;
            Rectangle rect = new Rectangle(1, 1, Width - 2, Height - 2);
            bool visible = rect.Contains(p);
            return visible;
        }

        /// <summary>
        /// Handles the mouse move event to update visibility of "clear text" button.
        /// </summary>
        public override bool DoMouseMove(Point p, MouseButtons button)
        {
            base.DoMouseMove(p, button);
            bool visibleBefore = btnCancel.Visible;
            btnCancel.Visible = isCancelVisible();
            if (btnCancel.Visible != visibleBefore) MakeMePaint(false, RenderMode.Invalidate);
            return true;
        }

        /// <summary>
        /// Handles the mouse enter event to update visibility of "clear text" button.
        /// </summary>
        public override void DoMouseEnter()
        {
            base.DoMouseEnter();
            bool visibleBefore = btnCancel.Visible;
            btnCancel.Visible = isCancelVisible();
            if (btnCancel.Visible != visibleBefore) MakeMePaint(false, RenderMode.Invalidate);
        }

        /// <summary>
        /// Handles the mouse leave event to update visibility of "clear text" button.
        /// </summary>
        public override void DoMouseLeave()
        {
            base.DoMouseLeave();
            bool visibleBefore = btnCancel.Visible;
            btnCancel.Visible = false;
            if (btnCancel.Visible != visibleBefore) MakeMePaint(false, RenderMode.Invalidate);
        }

        /// <summary>
        /// Handles the text box's mouse leave event to update visibility of "clear text" button.
        /// </summary>
        private void onTxtMouseLeave(object sender, EventArgs e)
        {
            // Pointer may leave text box but still be inside me
            if (sender == txtInput)
            {
                btnCancel.Visible = isCancelVisible();
                MakeMePaint(false, RenderMode.Invalidate);
            }
        }

        /// <summary>
        /// Handles the text box's mouse enter event to update visibility of "clear text" button.
        /// </summary>
        private void onTxtMouseEnter(object sender, EventArgs e)
        {
            btnCancel.Visible = txtInput.Text != string.Empty;
            MakeMePaint(false, RenderMode.Invalidate);
        }

        private void onTxtMouseMove(object sender, MouseEventArgs e)
        {
            btnCancel.Visible = txtInput.Text != string.Empty;
            MakeMePaint(false, RenderMode.Invalidate);
        }

        private void onTxtTextChanged(object sender, EventArgs e)
        {
            bool visibleBefore = btnCancel.Visible;
            btnCancel.Visible = isCancelVisible();
            if (btnCancel.Visible != visibleBefore) MakeMePaint(false, RenderMode.Invalidate);
        }

        /// <summary>
        /// Handles click on search button.
        /// </summary>
        private void onClickSearch(ZenControlBase sender)
        {
            doStartSearch();
        }

        /// <summary>
        /// Handles click on "clear text" button.
        /// </summary>
        private void onClickCancel(ZenControlBase sender)
        {
            txtInput.Text = "";
        }

        /// <summary>
        /// Does any required custom painting.
        /// </summary>
        public override void DoPaint(Graphics g)
        {
            // Paint my BG
            using (SolidBrush b = new SolidBrush(ZenParams.WindowColor))
            {
                g.FillRectangle(b, new Rectangle(0, 0, Width, Height));
            }
            // Draw my border
            using (Pen p = new Pen(ZenParams.BorderColor))
            {
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            }
            // Children! My buttons.
            DoPaintChildren(g);
        }

        /// <summary>
        /// Handles text box's key press event to catch "Enter" to trigger search.
        /// </summary>
        private void onTextBoxKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                doStartSearch();
                e.Handled = true;
            }
        }
    }
}
