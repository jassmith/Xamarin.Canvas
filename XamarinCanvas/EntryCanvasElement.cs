using System;
using System.Collections;
using System.Runtime.InteropServices;
using Gtk;

using MonoDevelop.Components;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace XamarinCanvas
{
	public class EntryCanvasElement : CanvasElement
	{
		bool drawCaret;

		string currentEntry;
		string CurrentEntry {
			get {
				return currentEntry;
			}
			set {
				if (currentEntry == value)
					return;
				currentEntry = value;
				QueueDraw ();
			}
		}

		int caretOffset;
		int CaretOffset {
			get {
				return caretOffset;
			}
			set {
				if (caretOffset == value)
					return;
				caretOffset = value;
				QueueDraw ();
			}
		}

		int caretHighlightOffset;
		int CaretHighlightOffset {
			get {
				return caretHighlightOffset;
			}
			set {
				if (value == CaretOffset)
					value = -1;
				caretHighlightOffset = value;
			}
		}

		public event EventHandler Activated;

		Gtk.IMContext imContext;
		public EntryCanvasElement ()
		{
			CurrentEntry = "";
			CaretOffset = 0;
			CaretHighlightOffset = -1;

			SetSize (200, 30);
			imContext = new IMMulticontext ();
			imContext.PreeditChanged += HandlePreeditChanged;
			imContext.PreeditStart += HandlePreeditStart;
			imContext.PreeditEnd += HandlePreeditEnd;
			imContext.RetrieveSurrounding += HandleRetrieveSurrounding;
			imContext.SurroundingDeleted += HandleSurroundingDeleted;
			imContext.Commit += HandleCommit;
		}

		void HighlightTo (int x, int y)
		{
			CaretHighlightOffset = PositionToOffset (x, y);
			if (CaretHighlightOffset == CaretOffset)
				CaretHighlightOffset = -1;
		}

		void HighlightLeft ()
		{
			if (CaretHighlightOffset == -1) {
				if (CaretOffset != 0)
					CaretHighlightOffset = CaretOffset - 1;
			} else {
				if (CaretHighlightOffset > 0)
					CaretHighlightOffset--;
				if (CaretHighlightOffset == CaretOffset)
					CaretHighlightOffset = -1;
			}
		}

		void HighlightRight ()
		{
			if (CaretHighlightOffset == -1) {
				if (CaretOffset != CurrentEntry.Length)
					CaretHighlightOffset = CaretOffset + 1;
			} else {
				if (CaretHighlightOffset < CurrentEntry.Length)
					CaretHighlightOffset++;
				if (CaretHighlightOffset == CaretOffset)
					CaretHighlightOffset = -1;
			}
		}

		void HighlightAll ()
		{
			CaretOffset = 0;
			CaretHighlightOffset = CurrentEntry.Length;
		}

		void HighlightBegin ()
		{
			CaretHighlightOffset = 0;
		}

		void HighlightEnd ()
		{
			CaretHighlightOffset = CurrentEntry.Length;
		}

		void CaretLeft ()
		{
			if (CaretHighlightOffset == -1) {
				CaretOffset = Math.Max (0, CaretOffset - 1);
			} else {
				CaretOffset = Math.Min (CaretOffset, CaretHighlightOffset);
				CaretHighlightOffset = -1;
			}
		}

		void CaretRight ()
		{
			if (CaretHighlightOffset == -1) {
				CaretOffset = Math.Min (CurrentEntry.Length, CaretOffset + 1);
			} else {
				CaretOffset = Math.Max (CaretOffset, CaretHighlightOffset);
				CaretHighlightOffset = -1;
			}
		}

		void CaretBegin ()
		{
			CaretOffset = 0;
			CaretHighlightOffset = -1;
		}

		void CaretEnd ()
		{
			CaretOffset = CurrentEntry.Length;
			CaretHighlightOffset = -1;
		}

		void HandleCommit (object o, CommitArgs args)
		{
			RemoveSelection ();
			CurrentEntry = CurrentEntry.Insert (CaretOffset, args.Str);
			CaretOffset += args.Str.Length;
			QueueDraw ();
		}

		void HandleSurroundingDeleted (object o, SurroundingDeletedArgs args)
		{
		}

		void HandleRetrieveSurrounding (object o, RetrieveSurroundingArgs args)
		{
			imContext.SetSurrounding (CurrentEntry, CaretOffset);
		}

		void HandlePreeditEnd (object sender, EventArgs e)
		{
		}

		void HandlePreeditStart (object sender, EventArgs e)
		{
		}

		void HandlePreeditChanged (object sender, EventArgs e)
		{
			QueueDraw ();
		}

		int PositionToOffset (int x, int y)
		{
			x -= 10;
			y = 1;
			using (Pango.Layout layout = GetLayout ()) {
				int index, trailing;
				if (layout.XyToIndex (Pango.Units.FromPixels (x), Pango.Units.FromPixels (y), out index, out trailing)) {
					return index;
				} else {
					if (x < 0)
						return 0;
					return CurrentEntry.Length;
				}
			}
		}

		protected override void OnLayoutOutline (Cairo.Context context)
		{
			context.Rectangle (0, 0, Width, Height);
		}

		protected override void OnRender (Cairo.Context context)
		{
			OnLayoutOutline (context);
			context.Color = new Cairo.Color (1, 1, 1);
			context.Fill ();

			using (var layout = GetLayout ()) {
				int w, h;
				layout.GetPixelSize (out w, out h);


				int textX = 10;
				int textY = ((int)Height - h) / 2;


				if (CaretHighlightOffset != -1) {
					Pango.Rectangle rightStrongRect, rightRect;
					layout.GetCursorPos (Math.Max (CaretOffset, CaretHighlightOffset), out rightStrongRect, out rightRect);

					Pango.Rectangle leftStrongRect, leftRect;
					layout.GetCursorPos (Math.Min (CaretOffset, CaretHighlightOffset), out leftStrongRect, out leftRect);

					context.Rectangle (textX + Pango.Units.ToPixels (leftRect.X), textY + Pango.Units.ToPixels (leftRect.Y), Pango.Units.ToPixels (rightRect.X + rightRect.Width - leftRect.X), Pango.Units.ToPixels (leftRect.Height));
					context.Color = new Cairo.Color (0.8, 0.9, 1);
					context.Fill ();
				}

				context.MoveTo (textX, textY);
				context.Color = new Cairo.Color (0, 0, 0);
				Pango.CairoHelper.ShowLayout (context, layout);

				if (CaretHighlightOffset == -1 && drawCaret) {
					Pango.Rectangle strongRect, weakRect;
					layout.GetCursorPos (CaretOffset, out strongRect, out weakRect);
					context.Rectangle (textX + Pango.Units.ToPixels (weakRect.X), textY + Pango.Units.ToPixels (weakRect.Y), 1, Pango.Units.ToPixels (weakRect.Height));
					context.Color = new Cairo.Color (0, 0, 0);
					context.Fill ();
				}
			}
		}

		uint blinkHandler;
		protected override void OnFocusIn ()
		{
			imContext.FocusIn ();
			StartCaretBlink ();
		}

		protected override void OnFocusOut ()
		{
			imContext.FocusOut ();
			StopCaretBlink ();
		}

		void StartCaretBlink ()
		{
			if (blinkHandler != 0)
				StopCaretBlink ();

			drawCaret = true;
			blinkHandler = GLib.Timeout.Add (500, () => {
				drawCaret = !drawCaret;
				QueueDraw ();
				return true;
			});
			QueueDraw ();
		}

		void StopCaretBlink ()
		{
			if (blinkHandler != 0)
				GLib.Source.Remove (blinkHandler);
			drawCaret = false;
			QueueDraw ();
		}

		protected override void OnKeyPress (Gdk.EventKey evnt)
		{
			bool shifted = evnt.State.HasFlag (Gdk.ModifierType.ShiftMask);
			switch (evnt.Key) {
			case Gdk.Key.BackSpace:
				Backspace ();
				break;
			case Gdk.Key.Delete:
				Delete ();
				break;
			case Gdk.Key.Left:
				if (shifted)
					HighlightLeft ();
				else
					CaretLeft ();
				break;
			case Gdk.Key.Right:
				if (shifted)
					HighlightRight ();
				else
					CaretRight ();
				break;
			case Gdk.Key.Up:
			case Gdk.Key.Home:
				if (shifted)
					HighlightBegin ();
				else
					CaretBegin ();
				break;
			case Gdk.Key.Down:
			case Gdk.Key.End:
				if (shifted)
					HighlightEnd ();
				else
					CaretEnd ();
				break;
			case Gdk.Key.a:
				if (evnt.State.HasFlag (Gdk.ModifierType.MetaMask)) {
					HighlightAll ();
				}
				break;
			case Gdk.Key.ISO_Enter:
			case Gdk.Key.KP_Enter:
					OnActivated ();
				break;
			}

			imContext.FilterKeypress (evnt);
			if (HasFocus)
				StartCaretBlink ();

			QueueDraw ();
		}

		void RemoveSelection ()
		{
			if (CaretHighlightOffset == -1)
				return;

			int start = Math.Min (CaretOffset, CaretHighlightOffset);
			int length = Math.Abs (CaretOffset - CaretHighlightOffset);
			CurrentEntry = CurrentEntry.Remove (start, length);
			CaretOffset = start;
			CaretHighlightOffset = -1;
		}

		void Backspace ()
		{
			if (CaretHighlightOffset == -1) {
				if (CaretOffset > 0) {
					CurrentEntry = CurrentEntry.Remove (CaretOffset - 1, 1);
					CaretOffset = CaretOffset - 1;
				}
			} else {
				RemoveSelection ();
			}
		}

		void Delete ()
		{
			if (CaretHighlightOffset == -1) {
				if (CaretOffset != CurrentEntry.Length)
					CurrentEntry = CurrentEntry.Remove (CaretOffset, 1);
			} else {
				RemoveSelection ();
			}
		}

		protected override void OnMouseMotion (double x, double y, Gdk.ModifierType state)
		{
			if (state.HasFlag (Gdk.ModifierType.Button1Mask)) {
				HighlightTo ((int)x, (int)y);
				QueueDraw ();
			}
		}

		protected override void OnButtonPress (double x, double y, uint button, Gdk.ModifierType state)
		{
			if (button == 1) {
				int location = PositionToOffset ((int)x, (int)y);
				if (location >= 0) {
					CaretOffset = location;
					CaretHighlightOffset = -1;
				}
			}
			QueueDraw ();
		}

		protected override void OnKeyRelease (Gdk.EventKey evnt)
		{

		}

		Pango.Layout GetLayout ()
		{
			Pango.Layout result = new Pango.Layout (Canvas.PangoContext);
			result.SetText (CurrentEntry);
			return result;
		}

		protected override void OnClicked (double x, double y, Gdk.ModifierType state)
		{
			 if (state.HasFlag (Gdk.ModifierType.Button3Mask)) {
				Random r = new Random ();
				
				RotateTo (r.Next (-10, 10) * r.NextDouble (), 1000, Easing.SpringOut);
				CurveTo (r.Next (0, Canvas.Allocation.Width), r.Next (0, Canvas.Allocation.Height), 
				         r.Next (0, Canvas.Allocation.Width), r.Next (0, Canvas.Allocation.Height), 
				         r.Next (0, Canvas.Allocation.Width), r.Next (0, Canvas.Allocation.Height), 
				         1000, Easing.CubicInOut);
				ScaleTo ((0.1 + r.NextDouble ()) * 3, 1000, Easing.BounceOut);
			}
		}

		void OnActivated ()
		{
			if (Activated != null)
				Activated (this, EventArgs.Empty);
		}
	}
	
}
