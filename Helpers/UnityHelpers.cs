using System.Linq;
using UnityEngine;

public static class UnityHelpers
{
	public static T FindSingleInstanceObject<T>() where T : MonoBehaviour {
		return Resources.FindObjectsOfTypeAll<Transform>()
			.Select(t => t.GetComponent<T>())
			.Single(c => c != null);
	}

	public static void ScaleMeshVertices(Mesh mesh, float xScale = 1f, float yScale = 1f, float zScale = 1f) {
		var vertices = new Vector3[mesh.vertices.Length];
		for (int i = 0; i < mesh.vertices.Length; i++) {
			var vertex = mesh.vertices[i];

			vertex.x *= xScale;
			vertex.y *= yScale;
			vertex.z *= zScale;

			vertices[i] = vertex;
		}

		mesh.vertices = vertices;
	}
}
