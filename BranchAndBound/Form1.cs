using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TSP.ModelTSP;

namespace BranchAndBound
{
	public partial class Form1 : Form
	{
		private readonly CancellationTokenSource[] cts;
		private readonly Stack<TextBox> errorTB;

		private readonly Queue<Task> tasks;
		private Cities cities;

		public Form1()
		{
			InitializeComponent();
			errorTB = new Stack<TextBox>();
			tasks = new Queue<Task>();
			cts = new CancellationTokenSource[5];
			for (var i = 0; i < cts.Length; i++)
			{
				cts[i] = new CancellationTokenSource();
			}
		}

		private void CancelCalculation()
		{
			foreach (var c in cts)
			{
				c.Cancel();
			}
			tasks.Clear();
		}

		public void DrawPoints()
		{
			CancelCalculation();
			ClearTextBox();
			cities = new Cities(dataGridView1.RowCount - 1);
			var points = new List<Location>();
			for (var i = 0; i < dataGridView1.RowCount - 1; i++)
			{
				var location = new Location
				{
					X = int.Parse(dataGridView1["X", i].Value.ToString()),
					Y = int.Parse(dataGridView1["Y", i].Value.ToString())
				};
				points.Add(location);
			}
			cities.Generate(points);
			var image = new Bitmap(graph5.Width-2, graph5.Height-2);
			var g = Graphics.FromImage(image);
			DrawPoints(g);
			graph5.Image = image;
		}

		public void DrawPoints(Graphics graphics)
		{
			graphics.SmoothingMode = SmoothingMode.HighQuality;

			for (var i = 0; i < cities.NumCities; i++)
			{
				var l = cities.GetLocation(i);
				// формирование точек на карте
				var city = new Rectangle();
				city.Location = new Point(l.X - 4, l.Y - 4);
				city.Width = 8;
				city.Height = 8;
				graphics.DrawEllipse(new Pen(Brushes.Black, 1), city);
				graphics.FillEllipse(Brushes.Gray, city);
			}
		}

		public void DrawLines(Location[] trail, PictureBox graph)
		{
			var image = new Bitmap(graph5.Width-2, graph5.Height-2);
			var g = Graphics.FromImage(image);

			g.SmoothingMode = SmoothingMode.HighQuality;
			var city = 1;
			for (; city < trail.Length; city++)
			{
				g.DrawLine(new Pen(Color.Purple, 3), new Point(trail[city - 1].X, trail[city - 1].Y),
					new Point(trail[city].X, trail[city].Y));
			}
			g.DrawLine(new Pen(Color.Purple, 3), new Point(trail[city - 1].X, trail[city - 1].Y),
				new Point(trail[0].X, trail[0].Y));

			DrawPoints(g);
			graph.Image = image;
		}

		public void ClearTextBox()
		{
			while (errorTB.Count > 0)
			{
				errorTB.Pop().BackColor = Color.White;
			}
		}


		public async Task CalculateBB()
		{
			ClearTextBox();
			cts[4].Cancel();
			cts[4] = new CancellationTokenSource();
			var maxTime = 5;
			TSP.ModelTSP.BranchAndBound algorithm = null;
			Stopwatch time = null;
			Location[] solve = null;
			var thisTask = Task.Run(() =>
			{
				algorithm = new TSP.ModelTSP.BranchAndBound();
				time = new Stopwatch();
				time.Start();
				solve = algorithm.Solution(cities);
				time.Stop();
			});
			tasks.Enqueue(thisTask);
			await Task.Run(() =>
			{
				while (true)
				{
					if (cts[4].Token.IsCancellationRequested || thisTask.IsCompleted)
					{
						break;
					}
				}
			});
			if (solve != null)
			{
				DrawLines(solve, graph5);
				lengthBB.Text = Math.Round(algorithm.TotalDistance, 2).ToString();
				dataGridView1.Rows.Clear();
				for (int i = 0; i < solve.Length; i++)
				{
					dataGridView1.Rows.Add(solve[i].X, solve[i].Y);
				}
				dataGridView1.Refresh();
			}
		}


		private void button_CalcBB_Click(object sender, EventArgs e)
		{
			DrawPoints();
			CalculateBB();
		}
	}
}