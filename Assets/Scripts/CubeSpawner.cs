using UnityEngine;

public class CubeSpawner : MonoBehaviour
{
    public GameObject cubePrefab;
    private GameObject currentCube;

    void Start()
    {
        SpawnCube();
    }

    void SpawnCube()
    {
        currentCube = Instantiate(cubePrefab, transform.position, Quaternion.identity);
        currentCube.GetComponent<Renderer>().material.color = Random.ColorHSV();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == currentCube)
        {
            SpawnCube();
        }
    }
}