using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TSP.ModelTSP
{
	internal class BranchAndBound
	{
		private const int DEFAULT_SEED = -1;

		private class City
		{
			public City(double x, double y)
			{
				X = x;
				Y = y;
			}

			public double X { get; }

			public double Y { get; }

			/// <summary>
			///     how much does it cost to get from this to the destination?
			///     note that this is an asymmetric cost function
			/// </summary>
			/// <param name="destination">um, the destination</param>
			/// <returns></returns>
			public double costToGetTo(City destination)
			{
				return Math.Sqrt(Math.Pow(X - destination.X, 2) + Math.Pow(Y - destination.Y, 2));
			}
		}

		private class TSPSolution
		{
			/// <summary>
			///     we use the representation [cityB,cityA,cityC]
			///     to mean that cityB is the first city in the solution, cityA is the second, cityC is the third
			///     and the edge from cityC to cityB is the final edge in the path.
			///     you are, of course, free to use a different representation if it would be more convenient or efficient
			///     for your node data structure and search algorithm.
			/// </summary>
			public readonly ArrayList
				Route;

			public TSPSolution(ArrayList iroute)
			{
				Route = new ArrayList(iroute);
			}


			/// <summary>
			///     compute the cost of the current route.  does not check that the route is complete, btw.
			///     assumes that the route passes from the last city back to the first city.
			/// </summary>
			/// <returns></returns>
			public double costOfRoute()
			{
				// go through each edge in the route and add up the cost. 
				int x;
				City here;
				var cost = 0D;

				for (x = 0; x < Route.Count - 1; x++)
				{
					here = Route[x] as City;
					cost += here.costToGetTo(Route[x + 1] as City);
				}
				// go from the last city to the first. 
				here = Route[Route.Count - 1] as City;
				cost += here.costToGetTo(Route[0] as City);
				return cost;
			}
		}

		private class TSPState
		{
			public TSPState(double Bound, List<int> ChildList, double[,] Matrix, List<int> PathSoFar,
				double Cost, int City, int TreeDepth)
			{
				this.Bound = Bound;
				this.ChildList = ChildList;
				this.Matrix = Matrix;
				this.PathSoFar = PathSoFar;
				this.Cost = Cost;
				this.City = City;
				this.TreeDepth = TreeDepth;

				if (this.ChildList.Count == 0)
					IsSolution = true;
				else
					IsSolution = false;
			}

			public double Bound { get; set; }

			public List<int> ChildList { get; }

			public double[,] Matrix { get; }

			public List<int> PathSoFar { get; }

			public double Cost { get; set; }

			public int City { get; }

			public int TreeDepth { get; }

			public bool IsSolution { get; set; }
		}

		private class PriorityQueue
		{
			private readonly SortedDictionary<double, Queue> storage;

			public PriorityQueue()
			{
				storage = new SortedDictionary<double, Queue>();
				size = 0;
			}

			public int size { get; private set; }

			public bool IsEmpty()
			{
				// Check if empty: based on total_size because keys can have multiple states
				return size == 0;
			}

			public TSPState Dequeue()
			{
				if (IsEmpty())
				{
					throw new Exception("Please check that priorityQueue is not empty before dequeing");
				}
				// Get first item from the sorted dictionary
				var kv = storage.First();
				// Get the Queue from the val
				var q = kv.Value;
				// Then grab the first item from it
				var deq = (TSPState) q.Dequeue();

				// Remove if now empty
				if (q.Count == 0)
					storage.Remove(kv.Key);

				// Decrement the total size
				size--;

				// Return the state that's been dequeued
				return deq;
			}

			public void Enqueue(TSPState item, double prio)
			{
				// Check if key prio exists
				if (!storage.ContainsKey(prio))
				{
					// Add a new queue for key prio
					storage.Add(prio, new Queue());
				}
				// Enqueue state at prio in queue
				storage[prio].Enqueue(item);
				// Inc the total size of the agenda
				size++;
			}

			public bool Contains(TSPState state)
			{
				// Get the state's priority for lookup
				var prio = state.Bound;
				// If it doesn't exist, absolutely false
				if (!storage.ContainsKey(prio))
				{
					return false;
				}
				// Otherwise run contains on the inner Queue
				return storage[prio].Contains(state);
			}
		}

		#region Constructors

		public double TotalDistance { get; private set; }

		public Location[] Solution(Cities cities)
		{
			_seed = 0;
			_size = cities.NumCities;
			_cities = new City[_size];
			_route = new ArrayList(_size);
			_bssf = null;
			for (var i = 0; i < _size; i++)
				_cities[i] = new City(cities.GetLocation(i).X, cities.GetLocation(i).Y);
			solveProblem();
			var resault = new Location[_bssf.Route.Count];
			var j = 0;
			foreach (City loc in _bssf.Route)
			{
				resault[j] = new Location();
				resault[j].X = (int) loc.X;
				resault[j].Y = (int) loc.Y;
				j++;
			}
			TotalDistance = _bssf.costOfRoute();
			return resault;
		}

		#endregion

		#region private members

		/// <summary>
		///     the cities in the current problem.
		/// </summary>
		private City[] _cities;

		/// <summary>
		///     a route through the current problem, useful as a temporary variable.
		/// </summary>
		private ArrayList _route;

		/// <summary>
		///     best solution so far.
		/// </summary>
		private TSPSolution _bssf;

		/// <summary>
		///     keep track of the seed value so that the same sequence of problems can be
		///     regenerated next time the generator is run.
		/// </summary>
		private int _seed;

		/// <summary>
		///     number of cities to include in a problem.
		/// </summary>
		private int _size;


		#endregion

		#region private members.

		private int Size
		{
			get { return _size; }
		}

		private int Seed
		{
			get { return _seed; }
		}

		#endregion

		#region Private Methods

		#endregion

		#region Public Methods

		/// <summary>
		///     return the cost of the best solution so far.
		/// </summary>
		/// <returns></returns>
		private double costOfBssf()
		{
			if (_bssf != null)
				return _bssf.costOfRoute();
			return -1D;
		}

		/// <summary>
		///     solve the problem.  This is the entry point for the solver when the run button is clicked
		///     right now it just picks a simple solution.
		/// </summary>
		private PriorityQueue PQ;

		private double BSSF;
		private double currBound;
		private List<int> BSSFList;
		private double[] rowMins;

		private void solveProblem()
		{
			// Start off with some var power!
			_route = new ArrayList();
			var currIndex = 0;

			// Set our BSSF to 0, so we can create a new better one
			BSSFList = new List<int>();
			BSSFList.Add(0);

			// Begin with our first city
			_route.Add(_cities[currIndex]);

			// Use the nearest neighbor greedy algorithm
			// to find a random (naive) solution
			while (_route.Count < _cities.Length)
			{
				currIndex ++;
				_route.Add(_cities[currIndex]);
				BSSFList.Add(currIndex);
			}
			// Save solution
			_bssf = new TSPSolution(_route);
			BSSF = _bssf.costOfRoute();

			//Build matrix for initial state
			var initialMatrix = buildInitialMatrix();

			//Get the minimum cost for the remaining cities; kinda like bound 
			rowMins = getRowMins();

			//Generate list of children for initial state
			var initStateChildren = new List<int>();
			for (var i = 1; i < _cities.Length; i++)
				initStateChildren.Add(i);

			//Build initial state                                           
			var initialState = new TSPState(0, initStateChildren, initialMatrix, new List<int>(), 0, 0, 0);
			initialState.Bound += boundingFunction(initialState);

			//Set the bound 
			currBound = initialState.Bound;

			//Start our PQ and load with init state
			PQ = new PriorityQueue();
			PQ.Enqueue(initialState, initialState.Bound);

			//Run Branch and Bound 
			BranchAndBoundEngine();
		}

		private DateTime endTime;

		private void BranchAndBoundEngine()
		{

			// Run until the PQ is empty, we find an optimal solution, or time runs out
			while (!PQ.IsEmpty()  && BSSF > currBound)
			{
				// Get a state from the PQ
				var state = PQ.Dequeue();
				// Check to see if the state is worth evaluating
				if (state.Bound < BSSF)
				{
					// Generate the states children and iterate
					var children = generateChildren(state);
					foreach (var child in children)
					{
						// If the bound is worth investigating...
						if (child.Bound < _bssf.costOfRoute())
						{
							// Check for a solution and save
							if (child.IsSolution && child.Cost < BSSF)
							{
								// Save solution
								BSSF = child.Cost;
								BSSFList = child.PathSoFar;
							}
							// Otherwise assign the state's bound and Enqueue
							else
							{
								var bound = child.Bound;
								// Our bound of min cost path to destination + state bound
								foreach (var childIndex in child.ChildList)
									bound += rowMins[childIndex];
								PQ.Enqueue(child, bound);
							}
						}
					}
				}
				GC.Collect();
			}

			//
			// END BRANCH AND BOUND
			//  

			// Clear the route
			_route.Clear();
			// Save the BSSF route
			for (var i = 0; i < BSSFList.Count; i++)
				_route.Add(_cities[BSSFList[i]]);

			// Create our soltuion and assign
			_bssf = new TSPSolution(_route);
		}

		private List<TSPState> generateChildren(TSPState state)
		{
			// Create new state list
			var children = new List<TSPState>();
			// Iterate through the current child's children
			foreach (var child in state.ChildList)
			{
				// Copy values from parent state so we can modify
				var childList = new List<int>(state.ChildList);
				var pathSoFar = new List<int>(state.PathSoFar);
				var cost = _cities[state.City].costToGetTo(_cities[child]);
				var matrix = (double[,]) state.Matrix.Clone();

				// Remove child from child list
				childList.Remove(child);
				// Add the parent state city to the path so far
				pathSoFar.Add(state.City);

				// Reduce the matrix
				for (var j = 0; j <= matrix.GetUpperBound(0); j++)
					matrix[j, state.City] = double.MaxValue;

				// Create a new state
				var newState = new TSPState(state.Bound + state.Matrix[state.City, child], childList, matrix, pathSoFar,
					state.Cost + cost, child, state.TreeDepth + 1);
				// Update the bound
				newState.Bound += boundingFunction(newState);

				// Check for a soltuion
				if (newState.IsSolution)
				{
					// Mark state as a solution
					newState.Cost += _cities[newState.City].costToGetTo(_cities[0]);
					newState.PathSoFar.Add(newState.City);
				}

				// Add child to childrens state
				children.Add(newState);
			}

			// Returnt the list for later usage
			return children;
		}

		private double[,] buildInitialMatrix()
		{
			// Create a matrix
			var matrix = new double[_cities.Length, _cities.Length];
			for (var i = 0; i < _cities.Length; i++)
			{
				for (var j = 0; j < _cities.Length; j++)
				{
					if (i == j)
						// Assign infinity if i == j
						matrix[i, j] = double.MaxValue;
					else
					// Otherwise populate the matrix with real numbers
						matrix[i, j] = _cities[i].costToGetTo(_cities[j]);
				}
			}
			return matrix;
		}

		private double[] getRowMins()
		{
			// Create an array getting the min cost from cities
			var rowMins = new double[_cities.Length];
			for (var i = 0; i < _cities.Length; i++)
			{
				var rowMin = double.MaxValue;
				for (var j = 0; j < _cities.Length; j++)
				{
					if (i != j)
					{
						var currCost = _cities[i].costToGetTo(_cities[j]);
						if (currCost < rowMin)
							rowMin = currCost;
					}
				}
				rowMins[i] = rowMin;
			}

			return rowMins;
		}

		private double boundingFunction(TSPState state)
		{
			// Start with 0 vals and the state's matrix
			double bound = 0;
			double numRows = 0;
			var matrix = state.Matrix;

			// Reduce the matrix rows
			// Create a child city list
			var childList = new List<int>(state.ChildList);
			// Add the current city to the list, creating all cities
			childList.Add(state.City);

			// Iterate through state's 1D col or row
			for (var i = 0; i < childList.Count; i++)
			{
				// Look for the min cost
				var minCost = double.MaxValue;
				// Interage through the other state's 1D col or row
				for (var j = 0; j < state.ChildList.Count; j++)
				{
					// Check if cost is less than min...
					if (matrix[childList[i], state.ChildList[j]] < minCost)
					{
						// Update the min cost
						minCost = matrix[childList[i], state.ChildList[j]];
					}
				}

				// Then once you find min cost and it's not infinity...
				if (minCost < double.MaxValue)
				{
					// Reduce the matrix
					for (var j = 0; j < state.ChildList.Count; j++)
						matrix[childList[i], state.ChildList[j]] -= minCost;
					// Add the cost to the bound
					bound += minCost;
				}
				else
				// Mark as infinity
					numRows++;
			}

			// Reduce the matrix columns
			for (var i = 0; i < state.ChildList.Count; i++)
			{
				// Look for the min cost
				var minCost = double.MaxValue;
				// Iterate through each column
				for (var j = 0; j < childList.Count; j++)
				{
					// Update min cost if the cell's value is less
					if (matrix[childList[j], state.ChildList[i]] < minCost)
						minCost = matrix[childList[j], state.ChildList[i]];
				}
				// Now that we have min cost, see if it is less than infinity
				if (minCost < double.MaxValue)
					// If so, reduce the matrix column
					for (var j = 0; j < childList.Count; j++)
						matrix[childList[j], state.ChildList[i]] -= minCost;
				else
				// Mark as infinity
					numRows++;
			}

			// If entire matrix is infinity
			if (numRows >= matrix.GetUpperBound(0))
			{
				// Save solution
				state.IsSolution = true;
				state.Cost += _cities[1].costToGetTo(_cities[state.City]);
			}
			// Return 0 if it is a solution
			if (state.IsSolution)
				return 0;

			// Otherwise return the bound as normal
			return bound;
		}

		#endregion
	}
}