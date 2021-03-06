﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PathFinding {
	
	static float colliderRadius = 0;	

	/// <summary>
	/// first checks to see if the AI can move straight to the goal
	/// Returns the optimal path for an AI to navigate to it's goal form it's start. 
	/// This function additionally will seach the current scene for waypoints.
	/// </summary>
	/// <returns>The A star path.</returns>
	/// <param name="start">Start.</param>
	/// <param name="goal">Goal.</param>
	public static List<GameObject> ReturnAStarPath(GameObject start, GameObject goal, List<string> tagExc)
	{		
		if (RaycastAllWithExceptions(start, goal, tagExc))
		{		
			//Debug.Log("Raycast hit something");
			GameObject[] nodelist = GameObject.FindGameObjectsWithTag("Waypoint");
			List<GameObject> waypoints = new List<GameObject>();
			for (int i = 0; i < nodelist.Length; i++)
			{
				waypoints.Add(nodelist[i]);
			}
			colliderRadius = start.GetComponent<CircleCollider2D>().radius;
			return AStarPathFinding(start, waypoints, goal, tagExc);	
		}
		else 
		{
			//Debug.Log("nothing hit");
			if (goal == null)
			{
				return new List<GameObject>();
			}
			else
			{
				return new List<GameObject> { goal };
			}
		}
	}
		
	/// <summary>
	/// Returns the optimal path for an AI to navigate to it's goal form it's start based on the possible nodes/waypoints it can visit.
	/// </summary>
	/// <returns>The star path finding.</returns>
	/// <param name="start">Start.</param>
	/// <param name="waypoints">Waypoints.</param>
	/// <param name="goal">Goal.</param>
	public static List<GameObject> AStarPathFinding(GameObject start, List<GameObject> waypoints, GameObject goal, List<string> tagExc)
	{
		//initiallizing for run of Astar	
		List<GameObject> closedSet = new List<GameObject>();
		List<GameObject> openSet = new List<GameObject>();
		openSet.Add(start);
		Dictionary<GameObject, GameObject> cameFrom = new Dictionary<GameObject, GameObject>();

		//The Gameobject (node) is the key, the float is the distance to the object
		Dictionary<GameObject, float> gScore = new Dictionary<GameObject, float>();
		foreach (GameObject node in waypoints)
		{
			gScore.Add(node, 10000000f);
		}
		gScore.Add(start, 0f);
		gScore.Add(goal, 10000000f);

		Dictionary<GameObject, float> fScore = new Dictionary<GameObject, float>();
		foreach (GameObject node in waypoints)
		{
			fScore.Add(node, 10000000f);
		}
		fScore.Add(start, heuristic_cost_estimate(start, goal));
		fScore.Add(goal, 10000000f);

		List<GameObject> current = new List<GameObject>();
		int nextInd = 0;
		
		waypoints.Add(goal);
		while (openSet.Count > 0)
		{
			if (FindLowestHeuristicCost(openSet, goal) != null)
			{
				current.Add(FindLowestHeuristicCost(openSet, goal));
			}
	
			nextInd++;
			if (current[nextInd - 1] == goal)
			{
				return ReconstructPath(cameFrom, start, goal);
			}
			openSet.Remove(current[nextInd - 1]);
			closedSet.Add(current[nextInd - 1]);

			List<GameObject> neighboors = DetermineNeighboors(waypoints, current[nextInd - 1], tagExc);
			foreach (GameObject neighboor in neighboors)
			{
				if (closedSet.Contains(neighboor))
				{
					continue;		//neighboor has already been evaluated, ignore it
				}
				float tentative_gScore = gScore[current[nextInd - 1]] + Vector3.Distance(current[nextInd - 1].transform.position, neighboor.transform.position);
				if (!openSet.Contains(neighboor))
				{
					openSet.Add(neighboor);
				}
				else if (tentative_gScore >= gScore[neighboor])
				{
					continue;	 	//this is not a better path
				}
				cameFrom[neighboor] = current[nextInd - 1];
				gScore[neighboor] = tentative_gScore;
				fScore[neighboor] = gScore[neighboor] + heuristic_cost_estimate(neighboor, goal);
			}
		}
		return openSet;
	}

	/// <summary>
	/// Raycasts to every node in the nodelist from the start to determine if there is an object inbetween them. 
	/// If there is not it is a neighboor.
	/// </summary>
	/// <returns>The neighboors.</returns>
	/// <param name="nodeList">Node list.</param>
	/// <param name="start">Start.</param>
	static List<GameObject> DetermineNeighboors(List<GameObject> nodeList, GameObject start, List<string> tagExc)
	{
		List<GameObject> neighboors = new List<GameObject>();
		foreach (GameObject node in nodeList)
		{
			if (!RaycastAllWithExceptions(start, node, tagExc))
			{
				neighboors.Add(node);		//node is a neighboor because we can move start to the node
			}
		}
		return neighboors;
	}

	/// <summary>
	/// Finds the object that is closest to the goal and returns it
	/// </summary>
	/// <returns>The lowest heuristic cost.</returns>
	/// <param name="nodeList">Node list.</param>
	/// <param name="goal">Goal.</param>
	static GameObject FindLowestHeuristicCost(List<GameObject> nodeList, GameObject goal)
	{
		float cost = 100000000f;
		int lowestInd = 0;
		if (nodeList.Count == 0)
		{
			return null;	
		}
		for (int count = 0; count < nodeList.Count; count++)
		{
			float temp = heuristic_cost_estimate(nodeList[count], goal);
			if (temp < cost)
			{
				lowestInd = count;
				cost = temp;
			}
		}
		return  nodeList[lowestInd];
	}

	/// <summary>
	/// returns the distance from one gameobject to the other
	/// </summary>
	/// <returns>The cost estimate.</returns>
	/// <param name="obj1">Obj1.</param>
	/// <param name="obj2">Obj2.</param>
	static float heuristic_cost_estimate(GameObject obj1, GameObject obj2)
	{
		return Vector3.Magnitude(obj1.transform.position - obj2.transform.position);
	}
	
	/// <summary>
	/// Reconstructs the path taken by the Astar algorithm
	/// </summary>
	/// <returns>The path.</returns>
	/// <param name="cameFrom">The dictionary of nodes that point to each other, the path the algorithm took</param>
	/// <param name="start">Start.</param>
	/// <param name="goal">Goal.</param>
	static List<GameObject> ReconstructPath(Dictionary<GameObject, GameObject> cameFrom, GameObject start, GameObject goal)
	{
		List<GameObject> path = new List<GameObject>();
		path.Add(goal);
		GameObject temp = goal;
		while (temp != start)
		{
			temp = cameFrom[temp];
			path.Add(temp);
		}
		path.Reverse();
		return path;
	}

/// <summary>
/// Sends a Raycast all from the start gameobject to the end Gameobject. Returns True if it hit anything not in the exception list.
/// </summary>
/// <returns><c>true</c>, otherwise, <c>false</c> an object not on the exception list is hit</returns>
/// <param name="start">Start. - point beginning the raycast </param>
/// <param name="goal">Goal. - point ending the raycast</param>
/// <param name="tags">Tags - list of tags that are the exception list</param>
	public static bool RaycastAllWithExceptions(GameObject start, GameObject goal, List<string> tags)
	{
		if (goal == null)
		{
			return false;
		}

		Vector2 left = new Vector2(start.transform.position.x, start.transform.position.y);
		Vector2 right = new Vector2(goal.transform.position.x, goal.transform.position.y);
		Vector2 dir = (right - left).normalized;

		RaycastHit2D[] tempArr = Physics2D.RaycastAll(left, dir, Mathf.Abs(Vector2.Distance(left, right)));
		RaycastHit2D temp = tempArr[0];
		bool hitObj = false;
		foreach (RaycastHit2D hit in tempArr)
		{
			bool hitAnExeption = false;
			foreach (string tag in tags)
			{
				if (hit.transform.tag == tag)
				{
					hitAnExeption = true;
				}
			}
			if (hit.transform == goal.transform)
			{
				hitAnExeption = true;
			}
			if (hit.transform == start.transform)
			{
				hitAnExeption = true;
			}
			//can't hit the start, tags are exceptions, and if you can hit the goal, ignore that collision
			if (hitAnExeption == false)
			{
				temp = hit;
				hitObj = true;
//				Debug.Log("raycast from: start = " + start.name + " to " + goal.name + " .... hit: " + hit.transform.name);
			}
		}
		if (hitObj == true && temp != null && temp.distance < Mathf.Abs(Vector2.Distance(left, right)))
		{
			return true;
		} 
		return false;
	}

	#region might be able to remove all this
	private static Vector2[] GetWorstCaseColliderEdges(GameObject start, Vector2 dir)
	{
		Vector2[] worstPoints = new Vector2[2];
		Vector3 temp = new Vector3(dir.x, dir.y, 0);
		Vector3 cross = Vector3.Cross(temp, new Vector3(0, 0, 1));
		worstPoints[0] = new Vector2(cross.x, cross.y) * colliderRadius + new Vector2(start.transform.position.x, start.transform.position.y);
		worstPoints[1] = new Vector2(cross.x, cross.y) * -colliderRadius + new Vector2(start.transform.position.x, start.transform.position.y);
		return worstPoints;
	}

	/// <summary>
/// Sends a Raycast all from the start gameobject to the end Gameobject. Returns True if it hit anything not in the exception list.
/// </summary>
/// <returns><c>true</c>, otherwise, <c>false</c> an object not on the exception list is hit</returns>
/// <param name="start">Start. - point beginning the raycast </param>
/// <param name="goal">Goal. - point ending the raycast</param>
/// <param name="tags">Tags - list of tags that are the exception list</param>
	public static bool RaycastAllWithExceptionsFromCollider(GameObject start, GameObject goal, List<string> tags)
	{
		if (goal == null)
		{
			return false;
		}

		Vector2 left = new Vector2(start.transform.position.x, start.transform.position.y);
		Vector2 right = new Vector2(goal.transform.position.x, goal.transform.position.y);
		Vector2 dir = (right - left).normalized;

		Vector2[] startPoints = GetWorstCaseColliderEdges(start, dir);
		Vector2[] endPoints = GetWorstCaseColliderEdges(goal, dir);
		startPoints = new Vector2[]{startPoints[0], left, startPoints[1]};
		endPoints = new Vector2[]{endPoints[0], right, endPoints[1]};

		for (int k = 0; k < startPoints.Length; k++)
		{
			RaycastHit2D[] tempArr = Physics2D.RaycastAll(startPoints[k], dir, Mathf.Abs(Vector2.Distance(left, right)));
			RaycastHit2D temp = tempArr[0];
	
			bool hitObj = false;
			foreach (RaycastHit2D hit in tempArr)
			{
				bool hitAnExeption = false;
				foreach (string tag in tags)
				{
					if (hit.transform.tag == tag)
					{
						hitAnExeption = true;
					}
				}
				if (hit.transform == goal.transform)
				{
					hitAnExeption = true;
				}
				if (hit.transform == start.transform)
				{
					hitAnExeption = true;
				}
				//can't hit the start, tags are exceptions, and if you can hit the goal, ignore that collision
				if (hitAnExeption == false)
				{
					temp = hit;
					hitObj = true;
					Debug.Log("raycast from: start = " + start.name + " to " + goal.name + " .... hit: " + hit.transform.name);
				}
			}
			if (hitObj == true && temp != null && temp.distance < Mathf.Abs(Vector2.Distance(left, right)))
			{
				return true;
			} 
		}
		return false;
	}
	#endregion
}
