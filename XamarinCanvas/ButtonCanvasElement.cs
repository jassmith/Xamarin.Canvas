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

		public int InternalPadding { get; set; }

		public ButtonCanvasElement (CanvasElement child)
		{
			InputTransparent = false;
			InternalPadding = 10;

			this.child = child;
			child.InputTransparent = true;

			child.SizeChanged += (sender, e) => {
				UpdateLayout ();
			};

			Add (child);

			CanvasSet += (sender, e) => {
				UpdateLayout ();
			};

		}

		void UpdateLayout ()
		{
			var width = WidthRequest == -1 ? child.Width + 2 * InternalPadding : WidthRequest;
			var height = HeightRequest == -1 ? child.Height + 2 * InternalPadding : HeightRequest;
			SetSize (width, height);

			child.X = (Width - child.Width) / 2;
			child.Y = (Height - child.Height) / 2;
		}

		protected override void OnLayoutOutline (Cairo.Context context)
		{
			context.Rectangle (0, 0, Width, Height);
		}

		void CreateGradient (Cairo.LinearGradient lg)
		{
			if (State.HasFlag (ElementState.Pressed)) {
				lg.AddColorStop (0, new Cairo.Color (0.9, 0.9, 0.9));
				lg.AddColorStop (1, new Cairo.Color (1, 1, 1));
			} else if (State.HasFlag (ElementState.Prelight)) {
				lg.AddColorStop (0, new Cairo.Color (1, 1, 1));
				lg.AddColorStop (1, new Cairo.Color (0.95, 0.95, 0.95));
			} else {
				lg.AddColorStop (0, new Cairo.Color (1, 1, 1));
				lg.AddColorStop (1, new Cairo.Color (0.9, 0.9, 0.9));
			}

		}

		protected override void OnRender (Cairo.Context context)
		{
			context.RoundedRectangle (0.5, 0.5, Width - 1, Height - 1, 5);
			using (var lg = new Cairo.LinearGradient (0, 0, 0, Height)) {
				CreateGradient (lg);
				context.Pattern = lg;
				context.FillPreserve ();
			}

			context.LineWidth = 1;
			context.Color = new Cairo.Color (0.8, 0.8, 0.8);
			context.Stroke ();
		}
	}
	
}
