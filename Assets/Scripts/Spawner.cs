using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject prefab;
    public float range;

    public int count;
    // Use this for initialization
    void Start()
    {

        //for (int i = 0; i < count; i++)
        //{
        //    Spawn();
        //}
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.W))
        {
            Spawn();
        }
    }

    void Spawn()
    {
        var horse = Instantiate<GameObject>(prefab);

        horse.transform.SetParent(this.transform);
        horse.transform.position = new Vector3(Random.value * range, 0, Random.value * range);
    }
}
