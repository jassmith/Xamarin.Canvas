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
	
	
	class MainClass
	{
		
		public static void Main (string[] args)
		{
			Application.Init ();
			MainWindow win = new MainWindow ();
			win.Resize (800, 800);
			
			Canvas canvas = new Canvas ();
			
			canvas.Realized += (sender, e) => {
				BoxCanvasElement box = new BoxCanvasElement (new Cairo.Color (0.8, 0.9, 1), 20, 20);
				box.X = 0;
				box.Y = 700;

				box.ClickEvent += (s, a) => {
					box.CurveTo (300, 600, 300, 600, 600, 700, 1000);
				};

				canvas.AddElement (box);
			};
			
			win.Add (canvas);
			
			win.ShowAll ();
			Application.Run ();
		}
	}
}
