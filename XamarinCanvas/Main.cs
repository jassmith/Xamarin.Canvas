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
	public class SearchBoxCanvasElement : GroupCanvasElement
	{
		LabelCanvasElement searchLabel;
		
		EntryCanvasElement searchEntry;
		EntryCanvasElement replaceEntry;

		ButtonCanvasElement searchButton;
		ButtonCanvasElement nextButton;
		ButtonCanvasElement prevButton;
		ButtonCanvasElement replaceButton;
		ButtonCanvasElement replaceAllButton;
		
		CanvasElement troughBackground;

		bool showReplace;
		public bool ShowReplace {
			get {
				return showReplace;
			}
			set {
				if (showReplace == value)
					return;
				showReplace = value;
				Layout (true, showReplace);
			}
		}

		public SearchBoxCanvasElement ()
		{
			showReplace = true;
			SetPreferedSize (700, 24);
			InputTransparent = true;
			Build ();
		}

		void Build ()
		{
			troughBackground = new CanvasElement ();
			Add (troughBackground);

			searchLabel = new LabelCanvasElement ("Search");
			searchLabel.XAlign = 0;
			searchLabel.YAlign = 0.5;
			Add (searchLabel);

			searchEntry = new EntryCanvasElement ();
			Add (searchEntry);

			replaceEntry = new EntryCanvasElement ();
			Add (replaceEntry);

			searchButton = new ButtonCanvasElement (new ImageCanvasElement (LoadPixbuf ("searchbox-search-16.png")));
			searchButton.ClickEvent += (sender, e) => ShowReplace = !ShowReplace;

			PrepButton (searchButton);
			searchButton.Relief = false;
			Add (searchButton);

			nextButton = new ButtonCanvasElement (new ImageCanvasElement (LoadPixbuf ("go-next-ltr.png")));
			prevButton = new ButtonCanvasElement (new ImageCanvasElement (LoadPixbuf ("go-previous-ltr.png")));
			PrepButton (nextButton);
			PrepButton (prevButton);
			Add (nextButton);
			Add (prevButton);

			replaceButton = new ButtonCanvasElement (new LabelCanvasElement ("Replace"));
			replaceAllButton = new ButtonCanvasElement (new LabelCanvasElement ("Replace All"));
			PrepButton (replaceButton);
			PrepButton (replaceAllButton);
			Add (replaceButton);
			Add (replaceAllButton);
		}

		protected override void OnChildPreferedSizeChanged (object sender, EventArgs e)
		{
			OnSizeAllocated (Width, Height);
		}

		protected override void OnSizeAllocated (double width, double height)
		{
			Layout (false, ShowReplace);
		}

		void SetPosition (CanvasElement element, double x, double y, bool animate)
		{
			if (animate) {
				element.MoveTo (x, y);
			} else {
				element.X = x;
				element.Y = y;
			}
		}

		void SizeChild (CanvasElement element, double width, double height, bool animate)
		{
			if (animate) {
				element.SizeTo (width, height);
			} else {
				element.SetSize (width, height);
			}
		}

		void Layout (bool animate, bool showReplace)
		{
			double x = 0;
			int searchLabelBuffer = 5;
			if (showReplace) {
				double nonEntryWidth = searchLabel.PreferedWidth + nextButton.PreferedWidth + prevButton.PreferedWidth + 
					replaceButton.PreferedWidth + replaceAllButton.PreferedWidth + searchButton.PreferedWidth + searchLabelBuffer;
				double entryWidth = (int) ((Math.Max (2, Width - nonEntryWidth)) / 2);

				SetPosition (searchButton, x, 0, animate);
				searchButton.SetSize (searchButton.PreferedWidth, PreferedHeight);
				x += searchButton.Width;
				
				SetPosition (searchLabel, x, 0, animate);
				searchLabel.SetSize (searchLabel.PreferedWidth, PreferedHeight);
				x += searchLabel.Width + searchLabelBuffer;
				
				SetPosition (searchEntry, x, 0, animate);
				SizeChild (searchEntry, entryWidth, PreferedHeight, animate);
				x += entryWidth;
				
				SetPosition (prevButton, x, 0, animate);
				prevButton.SetSize (prevButton.PreferedWidth, PreferedHeight);
				x += prevButton.Width;
				
				SetPosition (nextButton, x, 0, animate);
				nextButton.SetSize (nextButton.PreferedWidth, PreferedHeight);
				x += nextButton.Width;
				
				SetPosition (replaceEntry, x, 0, animate);
				replaceEntry.SetSize (entryWidth, PreferedHeight);
				x += entryWidth;
				
				SetPosition (replaceButton, x, 0, animate);
				replaceButton.SetSize (replaceButton.PreferedWidth, PreferedHeight);
				x += replaceButton.Width;
				
				SetPosition (replaceAllButton, x, 0, animate);
				replaceAllButton.SetSize (replaceAllButton.PreferedWidth, PreferedHeight);
				x += replaceAllButton.Width;
			} else {
				double nonEntryWidth = searchLabel.PreferedWidth + nextButton.PreferedWidth + prevButton.PreferedWidth + searchButton.PreferedWidth + searchLabelBuffer;
				double entryWidth = (int) ((Math.Max (2, Width - nonEntryWidth)));
				
				SetPosition (searchButton, x, 0, animate);
				searchButton.SetSize (searchButton.PreferedWidth, PreferedHeight);
				x += searchButton.Width;
				
				SetPosition (searchLabel, x, 0, animate);
				searchLabel.SetSize (searchLabel.PreferedWidth, PreferedHeight);
				x += searchLabel.Width + searchLabelBuffer;
				
				SetPosition (searchEntry, x, 0, animate);
				SizeChild (searchEntry, entryWidth, PreferedHeight, animate);
				x += entryWidth;
				
				SetPosition (prevButton, x, 0, animate);
				prevButton.SetSize (prevButton.PreferedWidth, PreferedHeight);
				x += prevButton.Width;
				
				SetPosition (nextButton, x, 0, animate);
				nextButton.SetSize (nextButton.PreferedWidth, PreferedHeight);
				x += nextButton.Width;
				
				SetPosition (replaceEntry, x, 0, animate);
				replaceEntry.SetSize (entryWidth, PreferedHeight);
				x += entryWidth;
				
				SetPosition (replaceButton, x, 0, animate);
				replaceButton.SetSize (replaceButton.PreferedWidth, PreferedHeight);
				x += replaceButton.Width;
				
				SetPosition (replaceAllButton, x, 0, animate);
				replaceAllButton.SetSize (replaceAllButton.PreferedWidth, PreferedHeight);
				x += replaceAllButton.Width;
			}

			replaceEntry.InputTransparent = !showReplace;
			replaceButton.InputTransparent = !showReplace;
			replaceAllButton.InputTransparent = !showReplace;
		}

		void PrepButton (ButtonCanvasElement button)
		{
			button.Rounding = 0;
			button.InternalPadding = 6;
		}

		Gdk.Pixbuf LoadPixbuf (string name)
		{
			return Gdk.Pixbuf.LoadFromResource (name);
		}

		protected override void OnRender (Cairo.Context context)
		{
			OnLayoutOutline (context);
			context.Clip ();
			base.OnRender (context);
		}

		protected override void OnLayoutOutline (Cairo.Context context)
		{
			context.RoundedRectangle (0, 0, Width, Height, Height / 2);
		}
	}

	class MainClass
	{

		public static void Main (string[] args)
		{
			Application.Init ();
			MainWindow win = new MainWindow ();
			win.Resize (800, 800);

			Random r1 = new Random ();
			Canvas canvas = new Canvas ();
	
			canvas.Realized += (sender, e) => {
				GroupCanvasElement group = new GroupCanvasElement ();
				
//				CanvasElement element = new BoxCanvasElement (new Cairo.Color (r1.NextDouble (), r1.NextDouble (), r1.NextDouble ()), 200, 200);
//				element.X = 200;
//				element.Y = 200;
//				element.Draggable = true;
//				group.Add (element);
//				
//				element = new BoxCanvasElement (new Cairo.Color (r1.NextDouble (), r1.NextDouble (), r1.NextDouble ()), 200, 200);
//				element.X = 200;
//				element.Y = 400;
//				element.Draggable = true;
//				group.Add (element);
//				
//				element = new BoxCanvasElement (new Cairo.Color (r1.NextDouble (), r1.NextDouble (), r1.NextDouble ()), 200, 200);
//				element.X = 400;
//				element.Y = 200;
//				element.Draggable = true;
//				group.Add (element);
//				
//				element = new EntryCanvasElement ();
//				element.X = 400;
//				element.Y = 400;
//				element.AnchorX = 100;
//				element.AnchorY = 15;
//				group.Add (element);
//				
//				LabelCanvasElement label = new LabelCanvasElement ("Testing 123");
//				label.XAlign = 0.5;
//				label.YAlign = 0.5;
//				
//				ButtonCanvasElement button = new ButtonCanvasElement (label);
//				button.X = 200;
//				button.Y = 200;
//				button.Draggable = true;
//				group.Add (button);
//
//				int i = 0;
//				button.ClickEvent += (obj, arg) => {
//					label.Text = "Clicked " + i++;
//				};
//
//				var pbuf = new Gdk.Pixbuf ("/Users/jason/Desktop/nat.jpg");
//				var image = new ImageCanvasElement (pbuf);
//
//				button = new ButtonCanvasElement (image);
//				button.X = 400;
//				button.Y = 200;
//				button.HeightRequest = 50;
//				button.Draggable = true;
//				group.Add (button);

				SearchBoxCanvasElement element = new SearchBoxCanvasElement ();
				element.WidthRequest = 500;
				element.HeightRequest = 20;
				group.Add (element);


				canvas.AddElement (group);
				//group.Rotation = 1;
				group.AnchorX = 400;
				group.AnchorY = 400;
			};

			win.Add (canvas);

			win.ShowAll ();
			Application.Run ();
		}
	}
}
