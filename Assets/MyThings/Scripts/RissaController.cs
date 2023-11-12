using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using System.Linq;
using DG.Tweening;
using UniRx.Async;

public class RissaController : MonoBehaviour
{

   [SerializeField] float distanceThresold = 5;

   [SerializeField] Vector2 byeYaDarlingAnimationStartPosOffset = new Vector2(2,0);

   private Animation animation;
   void Awake()
   {
      GetComponent<Draggable>().DragEnd.Subscribe(_ => OnRissaDragEnd()).AddTo(this);
      animation = GetComponent<Animation>();
   }

   void OnRissaDragEnd()
   {
      var closeCustomer = MainGameController.Instance.customersInScene.FirstOrDefault( c => GetDistance(c) < distanceThresold);
      if(closeCustomer && closeCustomer.customerRequestController.IsRequestPicked(CustomerRequestController.CustomerRequestType.Message))
      {
         StartByeYaDarlings(closeCustomer);
      }
      
   }

   async void StartByeYaDarlings(CustomerController customer)
   {
      var dest = customer.transform.position + (Vector3)byeYaDarlingAnimationStartPosOffset;
      dest.z = transform.position.z;
      await transform.DOMove(dest,0.2f).OnCompleteAsObservable().ToUniTask(this.GetCancellationTokenOnDestroy());
      animation.Play();
   }

   float GetDistance(CustomerController customer)
   {
      var distance = Vector2.Distance(transform.position,customer.transform.position);
      return distance;
   }

}
