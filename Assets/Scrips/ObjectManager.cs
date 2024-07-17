using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Jobs;

public class ObjectManager: MonoBehaviour
{
    public GameObject objectToSpawn; // 要生成的物体
    public Transform spawnPoint; // 生成物体的位置
    private float timeSinceLastSpawn; // 上次生成以来的时间
    
    public List<Transform> objTransform = new List<Transform>();
    public List<Transform> targetTransforms = new List<Transform>();
    public List<int> nextList = new List<int>();
    private NativeArray<Vector3> _tTransforms;
    void Start()
    {
        timeSinceLastSpawn = 0f;
        
        _tTransforms = new NativeArray<Vector3>(targetTransforms.Count, Allocator.Persistent);
        for (int i = 0; i < targetTransforms.Count; i++)
        {
            _tTransforms[i] = targetTransforms[i].position;
        }
        
    }

    void Update()
    {
        timeSinceLastSpawn += Time.deltaTime; // 更新时间

        if (timeSinceLastSpawn >= 1f) // 如果已经过去至少1秒
        {
            SpawnObject(); // 生成物体
            timeSinceLastSpawn = 0f; // 重置计时器
        }

        
        NativeArray<int> _targetIndex = new NativeArray<int>(nextList.Count,Allocator.TempJob);

        for (int i = 0; i < nextList.Count; i++)
        {
            _targetIndex[i] = nextList[i];
        }
        
        var patrolJob = new PatrolJob()
        {
            partolSpeed = 4f,
            DeltaTime = Time.deltaTime,
            targetIndex = _targetIndex,
            TargetTransform = _tTransforms
        };
        
        TransformAccessArray transformAccessArray = new TransformAccessArray(objTransform.ToArray(),5);
        patrolJob.Schedule(transformAccessArray,default).Complete();

        for (int i = 0; i < nextList.Count; i++)
        {
            nextList[i] = _targetIndex[i];
        }

        _targetIndex.Dispose();
        transformAccessArray.Dispose();

    }

    void SpawnObject()
    {
        objTransform.Add(Instantiate(objectToSpawn, spawnPoint.position, spawnPoint.rotation).transform); // 实例化物体
        nextList.Add(0);
    }
    
    
    public struct PatrolJob : IJobParallelForTransform
    {
        public float partolSpeed;
        public float DeltaTime;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Vector3> TargetTransform;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<int> targetIndex;
        
        public void Execute(int index, TransformAccess transform)
        {
            transform.position = Vector3.MoveTowards(transform.position, TargetTransform[targetIndex[index]], partolSpeed * DeltaTime);
            if (transform.position == TargetTransform[targetIndex[index]])
            {
                targetIndex[index] = (targetIndex[index] + 1) % TargetTransform.Length;
            }
            transform.rotation = Quaternion.Lerp(transform.rotation,Quaternion.LookRotation(TargetTransform[targetIndex[index]]-transform.position),DeltaTime);
        }
    }



}
