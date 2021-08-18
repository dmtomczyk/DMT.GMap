using System;
using System.Windows;
using GMap.NET;

namespace Demo.WindowsPresentation
{
   public class Dummy
   {

   }

   public struct PointAndInfo
   {
      public PointLatLng Point;
      public string Info;

      public PointAndInfo(PointLatLng point, string info)
      {
         Point = point;
         Info = info;
      }
   }
}
