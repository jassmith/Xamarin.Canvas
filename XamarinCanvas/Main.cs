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

		public SearchBoxCanvasElement ()
		{
			Build ();
			InputTransparent = true;
		}

		void Build ()
		{
			troughBackground = new CanvasElement ();
			Add (troughBackground);

			searchLabel = new LabelCanvasElement ("Search");
			Add (searchLabel);

			searchEntry = new EntryCanvasElement ();
			Add (searchEntry);

			replaceEntry = new EntryCanvasElement ();
			Add (replaceEntry);

			ImageCanvasElement forward = new ImageCanvasElement (LoadPixbuf ("go-next-ltr.png"));
			ImageCanvasElement backward = new ImageCanvasElement (LoadPixbuf ("go-previous-ltr.png"));

			nextButton = new ButtonCanvasElement (forward);
			prevButton = new ButtonCanvasElement (backward);
			PrepButton (nextButton);
			PrepButton (prevButton);
			Add (nextButton);
			Add (prevButton);

			LabelCanvasElement replace = new LabelCanvasElement ("Replace");
			LabelCanvasElement replaceAll = new LabelCanvasElement ("Replace All");

			replaceButton = new ButtonCanvasElement (replace);
			replaceAllButton = new ButtonCanvasElement (replaceAll);
			PrepButton (replaceButton);
			PrepButton (replaceAllButton);
			Add (replaceButton);
			Add (replaceAllButton);
		}

		protected override void OnSizeAllocated (double width, double height)
		{
			double x = 0;
			double nonEntryWidth = searchLabel.Width + nextButton.Width + prevButton.Width + replaceButton.Width + replaceAllButton.Width;
			double entryWidth = (Math.Min (2, width - nonEntryWidth)) / 2;

			searchLabel.X = 0;
			x += searchLabel.Width;

			searchEntry.X = x;
			searchEntry.WidthRequest = entryWidth;
			x += entryWidth;

			nextButton.WidthRequest = x;
			x += nextButton.Width;

			prevButton.X = x;
			x += prevButton.Width;

			replaceEntry.X = x;
			replaceEntry.WidthRequest = entryWidth;
			x += entryWidth;

			replaceButton.X = x;
			x += replaceButton.Width;

			replaceAllButton.X = x;
			x += replaceAllButton.Width;
		}

		void PrepButton (ButtonCanvasElement button)
		{
			button.HeightRequest = Height;
		}

		Gdk.Pixbuf LoadPixbuf (string name)
		{
			return Gdk.Pixbuf.LoadFromResource (name);
		}

		protected override void OnRender (Cairo.Context context)
		{

		}

		protected override void OnLayoutOutline (Cairo.Context context)
		{
			context.RoundedRectangle (0, 0, Width, Height, Height / 2);
		}

		protected override void OnPrepChildRender (Cairo.Context context)
		{
			OnLayoutOutline (context);
			context.Clip ();
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
				
				CanvasElement element = new BoxCanvasElement (new Cairo.Color (r1.NextDouble (), r1.NextDouble (), r1.NextDouble ()), 200, 200);
				element.X = 200;
				element.Y = 200;
				element.Draggable = true;
				group.Add (element);
				
				element = new BoxCanvasElement (new Cairo.Color (r1.NextDouble (), r1.NextDouble (), r1.NextDouble ()), 200, 200);
				element.X = 200;
				element.Y = 400;
				element.Draggable = true;
				group.Add (element);
				
				element = new BoxCanvasElement (new Cairo.Color (r1.NextDouble (), r1.NextDouble (), r1.NextDouble ()), 200, 200);
				element.X = 400;
				element.Y = 200;
				element.Draggable = true;
				group.Add (element);
				
				element = new EntryCanvasElement ();
				element.X = 400;
				element.Y = 400;
				element.AnchorX = 100;
				element.AnchorY = 15;
				group.Add (element);
				
				LabelCanvasElement label = new LabelCanvasElement ("Testing 123");
				label.XAlign = 0.5;
				label.YAlign = 0.5;
				
				ButtonCanvasElement button = new ButtonCanvasElement (label);
				button.X = 200;
				button.Y = 200;
				button.Draggable = true;
				group.Add (button);

				int i = 0;
				button.ClickEvent += (obj, arg) => {
					label.Text = "Clicked " + i++;
				};

				var pbuf = new Gdk.Pixbuf ("/Users/jason/Desktop/nat.jpg");
				var image = new ImageCanvasElement (pbuf);

				button = new ButtonCanvasElement (image);
				button.X = 400;
				button.Y = 200;
				button.Draggable = true;
				group.Add (button);

//				SearchBoxCanvasElement element = new SearchBoxCanvasElement ();
//				element.WidthRequest = 500;
//				group.Add (element);


				canvas.AddElement (group);
//				group.RotateTo (Math.PI * 40, 200000);
				group.AnchorX = 400;
				group.AnchorY = 400;
			};

			win.Add (canvas);

			win.ShowAll ();
			Application.Run ();
		}
	}
}
