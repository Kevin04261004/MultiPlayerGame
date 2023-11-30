using System;
using System.Collections.Generic;
using UnityEngine;

internal class MainThreadWorker: MonoBehaviour {
  internal static MainThreadWorker Instance { get; private set; }
  internal readonly Queue<Action> jobs = new();

  private void Awake() {
    if(Instance == null) {
      Instance = this;
      DontDestroyOnLoad(gameObject);
    } else {
      Destroy(gameObject);
    }
  }

  private void Update() {
    while(jobs.Count > 0) {
      jobs.Dequeue().Invoke();
    }
  }

  internal void EnqueueJob(Action job) {
    jobs.Enqueue(job);
  }
}