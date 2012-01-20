/*
	TUIO C# Demo - part of the reacTIVision project
	http://reactivision.sourceforge.net/

	Copyright (c) 2005-2009 Martin Kaltenbrunner <mkalten@iua.upf.edu

	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/

using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using TUIO;

	public class TuioDemo : Form, TuioListener
	{
		private TuioClient client;
		private Dictionary<long,TuioDemoObject> objectList;
		private Dictionary<long,TuioCursor> cursorList;
		private object cursorSync = new object();
		private object objectSync = new object();

		public static int width, height;
		private int window_width =  640;
		private int window_height = 480;
		private int window_left = 0;
		private int window_top = 0;
		private int screen_width = Screen.PrimaryScreen.Bounds.Width;
		private int screen_height = Screen.PrimaryScreen.Bounds.Height;

		private bool fullscreen;
		private bool verbose;

		SolidBrush blackBrush = new SolidBrush(Color.Black);
		SolidBrush whiteBrush = new SolidBrush(Color.White);

		SolidBrush grayBrush = new SolidBrush(Color.Gray);
		Pen fingerPen = new Pen(new SolidBrush(Color.Blue), 1);

		public TuioDemo(int port) {

			verbose = false;
			fullscreen = false;
			width = window_width;
			height = window_height;

			this.ClientSize = new System.Drawing.Size(width, height);
			this.Name = "TuioDemo";
			this.Text = "TuioDemo";

			this.Closing+=new CancelEventHandler(Form_Closing);
			this.KeyDown +=new KeyEventHandler(Form_KeyDown);

			this.SetStyle( ControlStyles.AllPaintingInWmPaint |
							ControlStyles.UserPaint |
							ControlStyles.DoubleBuffer, true);

			objectList = new Dictionary<long,TuioDemoObject>(128);
			cursorList = new Dictionary<long,TuioCursor>(128);

			client = new TuioClient(port);
			client.addTuioListener(this);
			client.connect();
		}

		private void Form_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e) {

 			if ( e.KeyData == Keys.F1) {
	 			if (fullscreen == false) {

					width = screen_width;
					height = screen_height;

					window_left = this.Left;
					window_top = this.Top;

					this.FormBorderStyle = FormBorderStyle.None;
		 			this.Left = 0;
		 			this.Top = 0;
		 			this.Width = screen_width;
		 			this.Height = screen_height;

		 			fullscreen = true;
	 			} else {

					width = window_width;
					height = window_height;

		 			this.FormBorderStyle = FormBorderStyle.Sizable;
		 			this.Left = window_left;
		 			this.Top = window_top;
		 			this.Width = window_width;
		 			this.Height = window_height;

		 			fullscreen = false;
	 			}
 			} else if ( e.KeyData == Keys.Escape) {
				this.Close();

 			} else if ( e.KeyData == Keys.V ) {
 				verbose=!verbose;
 			}

 		}

		private void Form_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			client.removeTuioListener(this);

			client.disconnect();
			System.Environment.Exit(0);
		}

		public void addTuioObject(TuioObject o) {
			lock(objectSync) {
				objectList.Add(o.getSessionID(),new TuioDemoObject(o));
			} if (verbose) Console.WriteLine("add obj "+o.getSymbolID()+" ("+o.getSessionID()+") "+o.getX()+" "+o.getY()+" "+o.getAngle());
		}

		public void updateTuioObject(TuioObject o) {
			lock(objectSync) {
				objectList[o.getSessionID()].update(o);
			}
			if (verbose) Console.WriteLine("set obj "+o.getSymbolID()+" "+o.getSessionID()+" "+o.getX()+" "+o.getY()+" "+o.getAngle()+" "+o.getMotionSpeed()+" "+o.getRotationSpeed()+" "+o.getMotionAccel()+" "+o.getRotationAccel());
		}

		public void removeTuioObject(TuioObject o) {
			lock(objectSync) {
				objectList.Remove(o.getSessionID());
			}
			if (verbose) Console.WriteLine("del obj "+o.getSymbolID()+" ("+o.getSessionID()+")");
		}

		public void addTuioCursor(TuioCursor c) {
			lock(cursorSync) {
				cursorList.Add(c.getSessionID(),c);
			}
			if (verbose) Console.WriteLine("add cur "+c.getCursorID() + " ("+c.getSessionID()+") "+c.getX()+" "+c.getY());
		}

		public void updateTuioCursor(TuioCursor c) {
			if (verbose) Console.WriteLine("set cur "+c.getCursorID() + " ("+c.getSessionID()+") "+c.getX()+" "+c.getY()+" "+c.getMotionSpeed()+" "+c.getMotionAccel());
		}

		public void removeTuioCursor(TuioCursor c) {
			lock(cursorSync) {
				cursorList.Remove(c.getSessionID());
			}
			if (verbose) Console.WriteLine("del cur "+c.getCursorID() + " ("+c.getSessionID()+")");
 		}

		public void refresh(TuioTime frameTime) {
			Invalidate();
		}

		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
			// Getting the graphics object
			Graphics g = pevent.Graphics;
			g.FillRectangle(whiteBrush, new Rectangle(0,0,width,height));

			// draw the cursor path
			if (cursorList.Count > 0) {
 			 lock(cursorSync) {
			 foreach (TuioCursor tcur in cursorList.Values) {
					List<TuioPoint> path = tcur.getPath();
					TuioPoint current_point = path[0];

					for (int i = 0; i < path.Count; i++) {
						TuioPoint next_point = path[i];
						g.DrawLine(fingerPen, current_point.getScreenX(width), current_point.getScreenY(height), next_point.getScreenX(width), next_point.getScreenY(height));
						current_point = next_point;
					}
					g.FillEllipse(grayBrush, current_point.getScreenX(width) - height / 100, current_point.getScreenY(height) - height / 100, height / 50, height / 50);
					Font font = new Font("Arial", 10.0f);
					g.DrawString(tcur.getCursorID() + "", font, blackBrush, new PointF(tcur.getScreenX(width) - 10, tcur.getScreenY(height) - 10));
				}
			}
		 }

			// draw the objects
			if (objectList.Count > 0)
			{
 				lock(objectSync) {
					foreach (TuioDemoObject tobject in objectList.Values) {
						tobject.paint(g);
					}
				}
			}
		}

		public static void Main(String[] argv) {
	 		int port = 0;
			switch (argv.Length) {
				case 1:
					port = int.Parse(argv[0],null);
					if(port==0) goto default;
					break;
				case 0:
					port = 3333;
					break;
				default:
					Console.WriteLine("usage: java TuioDemo [port]");
					System.Environment.Exit(0);
					break;
			}

			TuioDemo app = new TuioDemo(port);
			Application.Run(app);

		}
	}
