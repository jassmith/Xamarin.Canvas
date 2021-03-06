using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Gtk;

using MonoDevelop.Components;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace XamarinCanvas
{
	public class ButtonCanvasElement : GroupCanvasElement
	{
		CanvasElement child;

		public override Gdk.CursorType Cursor {
			get {
				return Relief ? base.Cursor : Gdk.CursorType.Hand1;
			}
		}

		public bool Relief { get; set; }
		public int Rounding { get; set; }

		int internalPadding;
		public int InternalPadding {
			get {
				return internalPadding;
			}
			set {
				internalPadding = value;
				UpdateLayout ();
			}
		}

		public ButtonCanvasElement (CanvasElement child) : this ()
		{
			SetChild (child);
		}

		protected ButtonCanvasElement ()
		{
			internalPadding = 10;
			Relief = true;
			Rounding = 5;
		}

		protected void SetChild (CanvasElement child)
		{
			this.child = child;
			child.InputTransparent = true;
			Add (child);
			
			UpdateLayout ();
			child.PreferedSizeChanged += (sender, e) => UpdateLayout ();
		}

		void UpdateLayout ()
		{
			double preferedWidth = InternalPadding * 2 + child.PreferedWidth;
			double preferedHeight = InternalPadding * 2 + child.PreferedHeight;

			SetPreferedSize (preferedWidth, preferedHeight);
		}

		protected override void OnLayoutOutline (Cairo.Context context)
		{
			context.Rectangle (0, 0, Width, Height);
		}

		protected override void OnSizeAllocated (double width, double height)
		{
			double childWidth = width - 2 * InternalPadding;
			double childHeight = height - 2 * InternalPadding;

			if (childWidth < child.PreferedWidth)
				childWidth = Math.Min (child.PreferedWidth, width);

			if (childHeight < child.PreferedHeight)
				childHeight = Math.Min (child.PreferedHeight, height);

			childWidth = Math.Min (child.PreferedWidth, childWidth);
			childHeight = Math.Min (child.PreferedHeight, childHeight);

			child.SetSize (childWidth, childHeight);
			child.X = (width - childWidth) / 2;
			child.Y = (height - childHeight) / 2;
		}

		public static void CreateGradient (Cairo.LinearGradient lg, ElementState state, double opacity)
		{
			if (state.HasFlag (ElementState.Pressed)) {
				lg.AddColorStop (0, new Cairo.Color (0.9, 0.9, 0.9, opacity));
				lg.AddColorStop (1, new Cairo.Color (1, 1, 1, opacity));
			} else if (state.HasFlag (ElementState.Prelight)) {
				lg.AddColorStop (0, new Cairo.Color (1, 1, 1, opacity));
				lg.AddColorStop (1, new Cairo.Color (0.95, 0.95, 0.95, opacity));
			} else {
				lg.AddColorStop (0, new Cairo.Color (1, 1, 1, opacity));
				lg.AddColorStop (1, new Cairo.Color (0.9, 0.9, 0.9, opacity));
			}

		}

		protected override void OnRender (Cairo.Context context)
		{
			context.RoundedRectangle (0.5, 0.5, Width - 1, Height - 1, Rounding);
			if (Relief) {
				using (var lg = new Cairo.LinearGradient (0, 0, 0, Height)) {
					CreateGradient (lg, State, Opacity);
					context.Pattern = lg;
					context.FillPreserve ();
				}
				
				context.LineWidth = 1;
				context.Color = new Cairo.Color (0.8, 0.8, 0.8, Opacity);
				context.StrokePreserve ();
			}
			context.Clip ();
			base.OnRender (context);
		}
	}
	
}
