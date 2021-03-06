﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MapUtils;
using static MapUtils.Type;
using static MapUtils.Dir;
using static MapUtils.MapConstants;

public class SurfaceMeshGenerator : MonoBehaviour
{
    private class SurfaceNode
	{
		public Vector3 vertex;
		public Vector2 uv;
		//public Color color;
		public int index;
		
		public SurfaceNode(float x, float y, float cell_size) 
		{
			vertex = new Vector3(x * cell_size, 0.0f, y * cell_size);
			uv = new Vector2(x * cell_size, y * cell_size);
			//color = SurfaceMeshGenerator.surface_color;
			index = -1;
		}
	}
	
	private class MasterSurfaceNode
	{
		public SurfaceNode upleft;
		public SurfaceNode middle;
		
		public MasterSurfaceNode(int x, int y, float cell_size)
		{
			upleft = new SurfaceNode(x, y, cell_size);
			middle = new SurfaceNode((x + 0.5f), (y + 0.5f), cell_size);
		}
	}
	
	//public static Color surface_color = Color.cyan;
	//public static Color edge_color = Color.green;
	
	private int[,] map;
	private float cell_size;
	private List<List<Cmd>> all_wall_cmds;
	private int width;
	private int height;
	private Vector3 offset;
	
	private MasterSurfaceNode[,] node_map;
	private List<Vector3> vertices;
	private List<Vector2> uvs;
	private List<Color> colors;
	private List<int> triangles;
	
	private MeshFilter mesh_target;
	private float texture_scale;
	
	void set_config_variables()
	{
		MapConfiguration config = GameObject.FindGameObjectWithTag("Map").GetComponent<MapConfiguration>();
		this.texture_scale = config.surface_texture_scale;
		this.cell_size = config.cell_size;
		this.mesh_target = GetComponent<MeshFilter>();
	}
	
	public void generate_surface_mesh(int[,] map, List<List<Cmd>> wall_cmds, Vector3 offset)
	{
		set_config_variables();
		this.map = map;
		this.all_wall_cmds = wall_cmds;
		this.width = map.GetLength(0);
		this.height = map.GetLength(1);
		this.offset = offset;
		
		this.vertices = new List<Vector3>();
		this.uvs = new List<Vector2>();
		this.colors = new List<Color>();
		this.triangles = new List<int>();
		
		generate_node_map();
		
		triangulate_solid_surfaces();
		triangulate_wall_surfaces();
		
		Mesh mesh = mesh_target.mesh;
		
		mesh.triangles = null;
		mesh.vertices = vertices.ToArray();
		mesh.uv = uvs.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.RecalculateNormals();
		
		//Debug.Log(vertices.Count);
		//Debug.Log(triangles.Count);	
	}
	
	void generate_node_map()
	{
		node_map = new MasterSurfaceNode[width+1, height+1];
		for (int x = 0; x < width+1; x++) {
			for (int y = 0; y < height+1; y++) {
				node_map[x,y] = new MasterSurfaceNode(x, y, cell_size);
			}
		}
	}
	
	void triangulate_solid_surfaces()
	{
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				if (map[x,y] >= FILLED) {
					SurfaceNode n00 = node_map[x + 0, y + 0].upleft;
					SurfaceNode n01 = node_map[x + 1, y + 0].upleft;
					SurfaceNode n10 = node_map[x + 0, y + 1].upleft;
					SurfaceNode n11 = node_map[x + 1, y + 1].upleft;
					
					index_vertices(n00, n01, n10, n11);
					
					triangles.Add(n10.index);
					triangles.Add(n11.index);
					triangles.Add(n01.index);
					
					triangles.Add(n00.index);
					triangles.Add(n10.index);
					triangles.Add(n01.index);
				}
			}
		}
	}
	
	void triangulate_wall_surfaces()
	{
		foreach(List<Cmd> wall_cmds in all_wall_cmds) {
			foreach(Cmd cmd in wall_cmds) {
				MasterSurfaceNode n00 = node_map[cmd.pos.x + 0, cmd.pos.y + 0];
				MasterSurfaceNode n01 = node_map[cmd.pos.x + 1, cmd.pos.y + 0];
				MasterSurfaceNode n10 = node_map[cmd.pos.x + 0, cmd.pos.y + 1];
				MasterSurfaceNode n11 = node_map[cmd.pos.x + 1, cmd.pos.y + 1];
				/*if (cmd.type == LINE) {
					switch (cmd.dir) {
						case LEFT: 
							color_edges(n11.upleft, n10.upleft); break;
						case UP: 
							color_edges(n10.upleft, n00.upleft); break;
						case RIGHT: 
							color_edges(n00.upleft, n01.upleft); break;
						case DOWN: 
							color_edges(n01.upleft, n11.upleft); break;
					}
				}*/
				if (cmd.type == CORNER) {
					switch (cmd.dir) {
						case LEFT: 
							triangulate_corner(n01.upleft, n11.upleft, n10.upleft); break;
						case UP: 
							triangulate_corner(n11.upleft, n10.upleft, n00.upleft); break;
						case RIGHT:
							triangulate_corner(n10.upleft, n00.upleft, n01.upleft); break;
						case DOWN:
							triangulate_corner(n00.upleft, n01.upleft, n11.upleft); break;
					}
				}
				if (cmd.type == ALLEY) {
					switch (cmd.dir) {
						case LEFT:
							triangulate_alley(n00.middle, n00.upleft, n01.upleft, n11.upleft, n10.upleft); break;
						case UP:
							triangulate_alley(n00.middle, n01.upleft, n11.upleft, n10.upleft, n00.upleft); break;
						case RIGHT:
							triangulate_alley(n00.middle, n11.upleft, n10.upleft, n00.upleft, n01.upleft); break;
						case DOWN:
							triangulate_alley(n00.middle, n10.upleft, n00.upleft, n01.upleft, n11.upleft); break;
					}
				}
			}
		}
	}
	
	void triangulate_corner(SurfaceNode n0, SurfaceNode n1, SurfaceNode n2)
	{
		index_vertices(n0, n1, n2);
		//color_edges(n0, n2);
		
		triangles.Add(n2.index);
		triangles.Add(n1.index);
		triangles.Add(n0.index);
	}
	
	void triangulate_alley(SurfaceNode n0, SurfaceNode n1, SurfaceNode n2, SurfaceNode n3, SurfaceNode n4)
	{
		index_vertices(n0, n1, n2, n3, n4);
		//color_edges(n0, n1, n4);
		
		triangles.Add(n2.index);
		triangles.Add(n1.index);
		triangles.Add(n0.index);
		
		triangles.Add(n3.index);
		triangles.Add(n2.index);
		triangles.Add(n0.index);
		
		triangles.Add(n4.index);
		triangles.Add(n3.index);
		triangles.Add(n0.index);
	}
	
	void index_vertices(params SurfaceNode[] nodes)
	{
		foreach(SurfaceNode node in nodes) {
			if (node.index == -1) {
				node.index = vertices.Count;
				vertices.Add(node.vertex - offset);
				uvs.Add(node.uv / texture_scale);
				//colors.Add(node.color);
			}
		}
	}
	
	/*
	void color_edges(params SurfaceNode[] nodes)
	{
		foreach (SurfaceNode node in nodes) {
			if (node.index == -1) {
				node.color = edge_color;
			}
			else {
				colors[node.index] = edge_color;
			}
		}
	}*/

}
