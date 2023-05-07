using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using MvCamCtrl.NET;
using CAM = MvCamCtrl.NET.MyCamera;

namespace CameraInterface
{
    class CameraInterface
    {
        CAM Camera;
        public CAM.MV_FRAME_OUT g_ImageBuffer;
        bool g_bExit;
        bool g_Grabbing;
        int nRet;
        public CameraInterface(CAM.MV_CC_DEVICE_INFO device)
        {
            Camera = new CAM();
            g_ImageBuffer = new CAM.MV_FRAME_OUT();

            nRet = Camera.MV_CC_CreateDevice_NET(ref device);
            if (CAM.MV_OK != nRet) {
                throw new Exception("Create cam fail");
            }

        }

        public void OpenDevice()
        {
            nRet = Camera.MV_CC_OpenDevice_NET();
            if (CAM.MV_OK != nRet)
            {
                throw new Exception("Open device failed:{0:x8}");
            }
        }
        public void getImageBuffer()
        {
            nRet = Camera.MV_CC_GetImageBuffer_NET(ref g_ImageBuffer, 1000);
            if (nRet != CAM.MV_OK)
            {
                StopGrab();
                throw new Exception("Grap image fail");
            }
            Camera.MV_CC_FreeImageBuffer_NET(ref g_ImageBuffer);
        }
        private void ReceiveImageWorkThread(object obj)
        {
            while (true)
            {
                getImageBuffer();
                if (g_bExit)
                {
                    break;
                }
            }
        }

        public void StartGrab()
        {
            nRet = Camera.MV_CC_StartGrabbing_NET();
            if (CAM.MV_OK != nRet)
            {
                throw new Exception("Start grabbing fail");
            }

            g_Grabbing = true;

        }


        public Bitmap getOneImageFrame()
        {
            if(g_Grabbing == false)
            {
                StartGrab();
            }
            getImageBuffer();
            if (g_Grabbing == true)
            {
                StopGrab();
            }
            IntPtr pData = g_ImageBuffer.pBufAddr;
            int fWidth = g_ImageBuffer.stFrameInfo.nWidth;
            int fHeight = g_ImageBuffer.stFrameInfo.nHeight;
            int fLen = (int)g_ImageBuffer.stFrameInfo.nFrameLen;
            byte[] frameByte = new byte[fLen];
            Marshal.Copy(pData, frameByte, 0, fLen);

            Bitmap bitmap = new Bitmap(fWidth, fHeight, PixelFormat.Format8bppIndexed);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, fWidth, fHeight), ImageLockMode.WriteOnly, bitmap.PixelFormat); 
            Marshal.Copy(frameByte, 0, bitmapData.Scan0, frameByte.Length);
            bitmap.UnlockBits(bitmapData); 
            ColorPalette palette = bitmap.Palette; 
            for (int i = 0; i < 256; i++) { 
                palette.Entries[i] = Color.FromArgb(i, i, i); 
            } 
            bitmap.Palette = palette;

            bitmap.Save("image.bmp", ImageFormat.Bmp);
            return bitmap;
        }

        public void getContinueImageFrame()
        {
            if (g_Grabbing == false)
            {
                StartGrab();
            }
            Thread hReceiveImageThreadHandle = new Thread(ReceiveImageWorkThread);
            hReceiveImageThreadHandle.Start();
            
        }


        public void StopGrab()
        {
            nRet = Camera.MV_CC_StopGrabbing_NET();
            if (CAM.MV_OK != nRet)
            {
                throw new Exception("Stop grabbing failed");
            }
            g_Grabbing = false;
        }

        public void CloseDevice()
        {
            Camera.MV_CC_CloseDevice_NET();
        }
    }

    class CameraList {

        CAM.MV_CC_DEVICE_INFO_LIST CamList;
        int nRet;
        int OK = CAM.MV_OK;
        public CameraList()
        {
            CamList = new CAM.MV_CC_DEVICE_INFO_LIST();
        }
        public void InitCameraList()
        {
            nRet = CAM.MV_CC_EnumDevices_NET(CAM.MV_GIGE_DEVICE | CAM.MV_USB_DEVICE, ref CamList);
            if (nRet != OK)
                throw new Exception("Init Camera list fail");
        }

        public CAM.MV_CC_DEVICE_INFO_LIST getCameraList()
        {
            return CamList;
        }

        public CAM.MV_CC_DEVICE_INFO getFirstCameraInfo()
        {
            CAM.MV_CC_DEVICE_INFO deviceInfo = new CAM.MV_CC_DEVICE_INFO();
            if(CamList.nDeviceNum > 0)
            {
                deviceInfo = (CAM.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(CamList.pDeviceInfo[0], typeof(CAM.MV_CC_DEVICE_INFO));
            } else
            {
                throw new Exception("No camera found");
            }
            return deviceInfo;
        }

        public CameraInterface getFirstCamera()
        {
            CAM.MV_CC_DEVICE_INFO deviceInfo = getFirstCameraInfo();
            CameraInterface cameraInterface = new CameraInterface(deviceInfo);
            return cameraInterface;
        }

    }
}
