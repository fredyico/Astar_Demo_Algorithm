using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PathMarker
{
    public MapLocation location;     // the location of the marker in the maze
    public float G; // the cost of moving to this marker
    public float H; // the estimated cost of reaching the goal from this marker
    public float F; // the sum of G and H
    public GameObject marker;   // the visual representation of the marker in the Unity scene
    public PathMarker parent;   // the parent marker in the path

    public PathMarker(MapLocation l, float g, float h, float f, GameObject marker, PathMarker p)
    {
        location = l;
        G = g;
        H = h;
        F = f;
        this.marker = marker;
        parent = p;
    }

    // override the Equals method to compare PathMarkers based on their location
    public override bool Equals(object obj)
    {
        if ((obj == null) || !this.GetType().Equals(obj.GetType()))
        {
            return false;
        }
        else
        {
            return location.Equals(((PathMarker)obj).location);
        }
    }

    // override the GetHashCode method to use a constant value
    public override int GetHashCode()
    {
        return 0;
    }
}




public class FindPathAStar : MonoBehaviour
{
    public Maze maze;   // the maze object that contains the map data
    public Material closedMaterial; // the material to use for closed markers
    public Material openMaterial;   // the material to use for closed markers

    List<PathMarker> open = new List<PathMarker>(); // the list of open markers
    List<PathMarker> closed = new List<PathMarker>();   // the list of closed markers

    public GameObject start; // the visual representation of the starting square
    public GameObject end; // the visual representation of the ending square
    public GameObject pathP; // the visual representation of the path

    PathMarker goalNode; // the PathMarker representing the goal square
    PathMarker startNode; // the PathMarker representing the starting square

    PathMarker lastPos; // the last PathMarker that was explored
    bool done = false; // flag indicating whether the search is complete

    void RemoveAllMarkers()
    {
        // find all GameObjects with the tag "marker" and destroy them
        GameObject[] markers = GameObject.FindGameObjectsWithTag("marker");
        foreach(GameObject m in markers)
        {
            Destroy(m);
        }
    }

    // sets up the search by creating the start and goal markers and adding the start marker to the open list
    void BeginSearch()
    {        // function to start the search
        done = false;
        RemoveAllMarkers();
        // generate a list of valid starting locations in the maze
        List<MapLocation> locations = new List<MapLocation>();
        for (int z = 1; z < maze.depth-1; z++)        
            for (int x = 1; x < maze.width - 1; x++)
            {
                if(maze.map[x,z] != 1)
                {
                    locations.Add(new MapLocation(x, z));
                }
            }
        locations.Shuffle();         // shuffle the locations and use the first two as start and goal

        // create a PathMarker for the starting location
        Vector3 startLocation = new Vector3(locations[0].x * maze.scale, 0, locations[0].z * maze.scale);
        startNode = new PathMarker(new MapLocation(locations[0].x, locations[0].z), 0,0,0, 
            Instantiate(start, startLocation, Quaternion.identity), null);

        // create a PathMarker for the goal location
        Vector3 goalLocation = new Vector3(locations[1].x * maze.scale, 0, locations[1].z * maze.scale);
        goalNode = new PathMarker(new MapLocation(locations[1].x, locations[1].z), 0, 0, 0,
    Instantiate(end, goalLocation, Quaternion.identity), null);

        open.Clear();
        closed.Clear();
        open.Add(startNode);
        lastPos = startNode;
    }

    // explore neighboring squares to the current square    
    void Search (PathMarker thisNode)
    {
        if(thisNode == null)// if the node is null, exit
        {
            return;
        }
        if (thisNode.Equals(goalNode)) // if the node is the goal, set done to true and exit
        {
            done = true;
            return;
        }
        foreach(MapLocation dir in maze.directions) // for each direction
        {
            MapLocation neighbour = dir + thisNode.location;    // calculate the location of the neighbour
            if (maze.map[neighbour.x, neighbour.z] == 1)    // if the neighbour is a wall, skip it
            {
                continue;
            }
            if (neighbour.x < 1 || neighbour.x >= maze.width || neighbour.z < 1 || neighbour.z >= maze.depth)   // if the neighbour is out of bounds, skip it
            {
                continue;
            }
            if (IsClosed(neighbour))    // if the neighbour is already closed, skip it
            {
                continue;
            }
            // calculate the G, H, and F scores for the neighbour
            float G = Vector2.Distance(thisNode.location.ToVector(), neighbour.ToVector()) + thisNode.G;
            float H = Vector2.Distance(neighbour.ToVector(), goalNode.location.ToVector());
            float F = G + H;

            // create a new path marker game object for the neighbour
            GameObject pathBlock = Instantiate(pathP, new Vector3(neighbour.x * maze.scale, 0, neighbour.z * maze.scale),
                Quaternion.identity);

            // update the text values of the path marker game object
            TextMesh[] values = pathBlock.GetComponentsInChildren<TextMesh>();
            values[0].text = "G: " + G.ToString("0.00");
            values[1].text = "H: " + H.ToString("0.00");
            values[2].text = "F: " + F.ToString("0.00");

            // update the G, H, and F values of the neighbour if it's already open
            if (!UpdateMarker(neighbour, G, H, F, thisNode))
            {
                // otherwise, add the neighbour to the open list with the new G, H, and F values
                open.Add(new PathMarker(neighbour, G, H, F, pathBlock, thisNode));
            }
        }

        // sort the open list by F score, then by H score
        open = open.OrderBy(p => p.F).ThenBy(n=>n.H).ToList<PathMarker>();
        // move the marker with the lowest F score from the open list to the closed list
        PathMarker pm = (PathMarker)open.ElementAt(0); 
        closed.Add(pm);

        open.RemoveAt(0);
        // change the material of the marker to indicate that it's closed
        pm.marker.GetComponent<Renderer>().material = closedMaterial;
        // set the last position visited to the newly closed marker
        lastPos = pm;
    }
    // updates the G, H, and F values of an open marker if the new values are lower
    bool UpdateMarker(MapLocation pos, float g, float h, float f, PathMarker prt)
    {
        foreach(PathMarker p in open)
        {
            if (p.location.Equals(pos))
            {
                p.G = g;
                p.H = h;
                p.F = f;
                p.parent = prt;
                return true;
            }
        }
        return false;
    }
    // checks if the given marker is in the closed list
    bool IsClosed(MapLocation marker)
    {
        foreach(PathMarker p in closed)
        {
            if (p.location.Equals(marker))
            {
                return true;
            }           
        }
        return false;
    }

    // gets the path from the last position visited to the start marker and creates markers for each step
    void GetPath()
    {
        RemoveAllMarkers();
        PathMarker begin = lastPos;

        while (!startNode.Equals(begin) && begin != null)
        {
            Instantiate(pathP, new Vector3(begin.location.x * maze.scale, 0, begin.location.z * maze.scale),
                Quaternion.identity);
            begin = begin.parent;
        }
        Instantiate(pathP, new Vector3(startNode.location.x * maze.scale, 0, startNode.location.z * maze.scale), 
            Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))        // start the search when "P" key is pressed
        {
            BeginSearch();
        }
        if (Input.GetKeyDown(KeyCode.C) && !done)       // perform one step of the search when "C" key is pressed
        {
            Search(lastPos);
        }
        if (Input.GetKeyDown(KeyCode.M))        // display the path when "M" key is pressed
        {
            GetPath();
        }
    }
}
