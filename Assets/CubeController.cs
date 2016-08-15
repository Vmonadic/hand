using UnityEngine;
using System.Collections;
using System;
using System.IO.Ports;
using System.Threading;

public class IMU
{
    private static SerialPort serialPort = new SerialPort();
    public Vector3 currentOrient;
    private Vector3 prevAngles;
    public Vector3 initialOrient;

    public IMU(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits, Handshake handshake)
    {
        serialPort.PortName = portName;
        serialPort.BaudRate = baudRate;
        serialPort.Parity = parity;
        serialPort.DataBits = dataBits;
        serialPort.Handshake = handshake;
        serialPort.ReadTimeout = 500;
        serialPort.WriteTimeout = 500;
        serialPort.Open();
        currentOrient = new Vector3(0.0f, 0.0f, 0.0f);

        prevAngles = new Vector3(0.0f, 0.0f, 0.0f);
        serialPort.Write("#ob");
        serialPort.DiscardInBuffer();
        serialPort.DiscardOutBuffer();

        /*
        //Gets initial orientation
        serialPort.Write("#f");
        Thread.Sleep(100);
        byte[] bytes = new byte[4];
        float[] angles = new float[3];
        for (int i = 0; i < 3; i++)
        {
            bytes[0] = (byte)serialPort.ReadByte();
            bytes[1] = (byte)serialPort.ReadByte();
            bytes[2] = (byte)serialPort.ReadByte();
            bytes[3] = (byte)serialPort.ReadByte();
            angles[i] = BitConverter.ToSingle(bytes, 0);
            bytes[0] = bytes[1] = bytes[2] = bytes[3] = (byte)0;
        }
        initialOrient = new Vector3(angles[2], angles[0], angles[1]);
        Debug.Log(initialOrient);
        */
    }

    public Vector3 GetYaw() //writes and then reads one value
    {
        serialPort.Write("#f");
        Thread.Sleep(20);
        byte[] bytes = new byte[4];
        float[] angles = new float[3];
        for (int i = 0; i < 3; i++)
        {
            bytes[0] = (byte)serialPort.ReadByte();
            bytes[1] = (byte)serialPort.ReadByte();
            bytes[2] = (byte)serialPort.ReadByte();
            bytes[3] = (byte)serialPort.ReadByte();
            angles[i] = BitConverter.ToSingle(bytes, 0);
            bytes[0] = bytes[1] = bytes[2] = bytes[3] = (byte)0;
        }

        Vector3 outs = new Vector3();
        outs.x = angles[1];
        outs.y = angles[0];
        outs.z = angles[2];

        return outs;
    }

    public Vector3 GetCurrentOrient() //returns absolute position of bot, also stores it in currentOrient
    {
        Vector3 angles = GetYaw(); //raw yaw reading
        Vector3 delta = new Vector3(0.0f, 0.0f, 0.0f); //measures change in angle
        angles.z = 180.0f - angles.z;
        angles.x = 180.0f - angles.x;
        delta = angles - prevAngles;
        if (angles.x < 5.0f && prevAngles.x > 355.0f)
        {
            delta.x = angles.x + Math.Abs(360.0f - prevAngles.x);
        }

        else if (angles.x > 355.0f && prevAngles.x < 5.0f) //result of a very long and as-yet-inexplicable calculation
        {
            delta.x = -1 * (Math.Abs(360 - angles.x) + Math.Abs(prevAngles.x));
        } //ultra jugaad


        if (angles.y < 5.0f && prevAngles.y > 355.0f)
        {
            delta.y = angles.y + Math.Abs(360.0f - prevAngles.y);
        }

        else if (angles.y > 355.0f && prevAngles.y < 5.0f) //result of a very long and as-yet-inexplicable calculation
        {
            delta.y = -1 * (Math.Abs(360 - angles.y) + Math.Abs(prevAngles.y));
        } //ultra jugaad


        if (angles.z < 5.0f && prevAngles.z > 355.0f)
        {
            delta.z = angles.z + Math.Abs(360.0f - prevAngles.z);
        }

        else if (angles.z > 355.0f && prevAngles.z < 5.0f) //result of a very long and as-yet-inexplicable calculation
        {
            delta.z = -1 * (Math.Abs(360 - angles.z) + Math.Abs(prevAngles.z));
        } //ultra jugaad

        currentOrient = currentOrient + delta;
        prevAngles = angles;
        return currentOrient;
    }

    public void Update()
    {
        Vector3 useless;
        int k = 0;
        while (true)
        {
            useless = GetCurrentOrient();
            if (k < 10)
            {
                k++;
            }
            else if (k == 10)
            {
                initialOrient = GetCurrentOrient();
                k = 11;
            }
        }
    }
}



public class CubeController : MonoBehaviour {

    private IMU imu;
    public String portName;
    public float Kp;
    public float PThreshold;

    Thread imuReadThread;
	// Use this for initialization
	void Start () 
    {
        imu = new IMU(portName
            , 57600
            , (Parity)Enum.Parse(typeof(Parity), "None", true)
            , 8
            , (StopBits)Enum.Parse(typeof(StopBits), "One", true)
            , (Handshake)Enum.Parse(typeof(Handshake), "None", true));
        imuReadThread = new Thread(imu.Update);
        imuReadThread.Start();
	}
	
	// LateUpdate is called once per frame
	void LateUpdate () 
    {
        transform.rotation = Quaternion.Euler(imu.currentOrient - imu.initialOrient);// - imu.initialOrient);
	}
}
