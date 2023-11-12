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
   [SerializeField] Vector2 byeYaDarlingJailbirdFlyOffset = new Vector2(10,0);

   [SerializeField] float byeYaDarlingPart1Duration = 0.2f;
   [SerializeField] float byeYaDarlingPart2Duration = 0.2f;


   private Animation animation;

   Vector3 initPosition;
   State state;
   public enum State
   {
      Idle,
      ByeYaDarling
   }
   void Awake()
   {
      GetComponent<Draggable>().DragEnd.Subscribe(_ => OnRissaDragEnd()).AddTo(this);
      animation = GetComponent<Animation>();
      initPosition = transform.position;
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
      if(state == State.ByeYaDarling)
         return;
      if(customer.state != CustomerController.State.Waiting)
         return;

      state = State.ByeYaDarling;
      customer.state = CustomerController.State.Flying;

      var dest = customer.transform.position + (Vector3)byeYaDarlingAnimationStartPosOffset;
      dest.z = transform.position.z;
      await transform.DOMove(dest,byeYaDarlingPart1Duration/2).OnCompleteAsObservable().ToUniTask(this.GetCancellationTokenOnDestroy());

      dest -= (Vector3)byeYaDarlingAnimationStartPosOffset;
      await transform.DOMove(dest,byeYaDarlingPart1Duration/2).OnCompleteAsObservable().ToUniTask(this.GetCancellationTokenOnDestroy());

      dest -= (Vector3)byeYaDarlingJailbirdFlyOffset;
      customer.transform.DOMove(dest,byeYaDarlingPart2Duration);
      await customer.customerRenderer.transform.DOLocalRotate(new Vector3(0,0,360),byeYaDarlingPart2Duration,RotateMode.LocalAxisAdd)
      .OnCompleteAsObservable().ToUniTask(this.GetCancellationTokenOnDestroy());

      MainGameController.Instance.OnCustomerLeave(customer);

      await transform.DOMove(initPosition,byeYaDarlingPart2Duration).OnCompleteAsObservable().ToUniTask(this.GetCancellationTokenOnDestroy());
      state = State.Idle;
      // await customer.customerRenderer.transform.DOLocalRotateQuaternion(Quaternion.Euler(new Vector3(0,0,180)),byeYaDarlingPart2Duration);
      // animation.Play();
   }

   float GetDistance(CustomerController customer)
   {
      var distance = Vector2.Distance(transform.position,customer.transform.position);
      return distance;
   }

}
