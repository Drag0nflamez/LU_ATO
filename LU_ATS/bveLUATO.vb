'London Underground ATO Train Plugin for BVE

Imports System
Imports OpenBveApi

Public Class bveLUATO
    Implements IRuntime

    Dim AISupport As AISupport
    Dim InitializationModes As InitializationModes
    Dim Specifications As VehicleSpecs
    Dim signalaspect As SignalData
    Dim doorstate As DoorStates
    Dim atoactive As Boolean = False
    Dim speedlimit As Integer = 10
    Dim juicegap As Boolean = False
    Dim juicegaplength As Integer = 0
    Dim juicegapend As Integer = 0
    Dim juicegapstarted As Boolean = False
    Dim juicegapstartloc As Integer = 0
    Dim _reverser As Integer
    Dim power_notch As Integer
    Dim brake_notch As Integer
    Dim stationbrake As Boolean
    Dim speedlimit_track As Integer = 10

    Dim wantedpowernotch As Integer

    'station stopping via m/s braking rate.
    Dim oldtime As Integer
    Dim newtime As Integer
    Dim oldspeedms As Double
    Dim newspeedms As Double
    Dim alreadybreaking As Boolean


    Dim signalbrake As Boolean

    Dim debugmessage As String

    Dim Speed As Double

    Public Sub DoorChange(ByVal oldState As OpenBveApi.Runtime.DoorStates, ByVal newState As OpenBveApi.Runtime.DoorStates) Implements OpenBveApi.Runtime.IRuntime.DoorChange
        doorstate = newState
        If newState = DoorStates.None Then
        Else
            brake_notch = Specifications.BrakeNotches
        End If
    End Sub

    Public Sub Elapse(ByVal data As OpenBveApi.Runtime.ElapseData) Implements OpenBveApi.Runtime.IRuntime.Elapse
        If juicegap = True Then
            power_notch = 0
            If juicegapstarted = False Then
                juicegapstartloc = data.Vehicle.Location
                juicegapend = juicegapstartloc + juicegaplength
                juicegapstarted = True
                debugmessage = "Power rail gap. Not accelerating."
            Else
                If data.Vehicle.Location >= juicegapend Then
                    juicegap = False
                    juicegapstarted = False
                    data.Handles.PowerNotch = power_notch
                    debugmessage = "Train working perfectly fine."
                End If
            End If
        Else

        End If
        If atoactive = True Then
            'ato on logic
            If stationbrake = True Then
                power_notch = 0
                brake_notch = Specifications.BrakeNotches
                If data.Vehicle.Speed.KilometersPerHour < 1 Then
                    atoactive = False
                    stationbrake = False
                    alreadybreaking = False
                    oldtime = 0
                    newtime = 0
                    newspeedms = 0
                    oldspeedms = 0

                    debugmessage = "ATO stopped at station."
                End If
            Else
                If (data.Vehicle.Speed.KilometersPerHour > speedlimit) Then
                    power_notch = 0
                    brake_notch = Specifications.BrakeNotches
                    debugmessage = "Decelerating to " + speedlimit.ToString() + " kph"
                ElseIf juicegap = True Then
                    power_notch = 0
                ElseIf data.Vehicle.Speed.KilometersPerHour < speedlimit Then
                    power_notch = Specifications.PowerNotches
                    brake_notch = 0
                    debugmessage = "Accelerating to " + speedlimit.ToString() + " kph"
                End If
            End If
        Else
            If juicegap = True Then
                'do nothing
            Else
                If DoorsClosed() = True Then
                    power_notch = wantedpowernotch

                Else
                    power_notch = 0
                    brake_notch = Specifications.BrakeNotches
                End If
            End If
        End If
        data.Handles.PowerNotch = power_notch
        data.Handles.BrakeNotch = brake_notch
        data.Handles.Reverser = _reverser

        Speed = data.Vehicle.Speed.KilometersPerHour
        data.DebugMessage = debugmessage
    End Sub

    Public Sub HornBlow(ByVal type As OpenBveApi.Runtime.HornTypes) Implements OpenBveApi.Runtime.IRuntime.HornBlow

    End Sub

    Public Sub Initialize(ByVal mode As OpenBveApi.Runtime.InitializationModes) Implements OpenBveApi.Runtime.IRuntime.Initialize
        InitializationModes = InitializationModes.OnEmergency
        'doorstate = DoorStates.Both
    End Sub

    Public Sub KeyDown(ByVal key As OpenBveApi.Runtime.VirtualKeys) Implements OpenBveApi.Runtime.IRuntime.KeyDown
        If key = VirtualKeys.B1 And VirtualKeys.B2 Then
            StartATO()
        End If

        If key = VirtualKeys.A2 Then
            atoactive = False
        End If
    End Sub

    Public Sub KeyUp(ByVal key As OpenBveApi.Runtime.VirtualKeys) Implements OpenBveApi.Runtime.IRuntime.KeyUp

    End Sub

    Public Function Load(ByVal properties As OpenBveApi.Runtime.LoadProperties) As Boolean Implements OpenBveApi.Runtime.IRuntime.Load
        'we shall not support the AI, as it duplicates what I'm trying to do.
        AISupport = AISupport.None
        Return True

    End Function

    Public Sub PerformAI(ByVal data As OpenBveApi.Runtime.AIData) Implements OpenBveApi.Runtime.IRuntime.PerformAI
        data.Response = AIResponse.Long
    End Sub

    Public Sub SetBeacon(ByVal data As OpenBveApi.Runtime.BeaconData) Implements OpenBveApi.Runtime.IRuntime.SetBeacon
        If data.Type = 35 Then
            speedlimit_track = data.Optional
            If signalbrake = False Then
                speedlimit = data.Optional
            End If
        ElseIf (data.Type = 20) Then
            juicegap = True
            juicegaplength = data.Optional
        ElseIf (data.Type = 36) Then
            stationbrake = True
        End If
    End Sub

    Public Sub SetBrake(ByVal brakeNotch As Integer) Implements OpenBveApi.Runtime.IRuntime.SetBrake
        If DoorsClosed() = False Then
            brake_notch = Specifications.BrakeNotches
        Else
            brake_notch = brakeNotch
        End If
    End Sub

    Public Sub SetPower(ByVal powerNotch As Integer) Implements OpenBveApi.Runtime.IRuntime.SetPower
        If atoactive = False Then
            If DoorsClosed() = True Then
                wantedpowernotch = powerNotch
            End If
        End If
    End Sub

    Public Sub SetReverser(ByVal reverser As Integer) Implements OpenBveApi.Runtime.IRuntime.SetReverser
        _reverser = reverser
    End Sub

    Public Sub SetSignal(ByVal data() As OpenBveApi.Runtime.SignalData) Implements OpenBveApi.Runtime.IRuntime.SetSignal
        If data(1).Distance <= 200 And data(1).Aspect = 0 Then
            speedlimit = 0
            signalbrake = True
        Else
            speedlimit = speedlimit_track
            signalbrake = False
        End If
        signalaspect = data(1)
    End Sub

    Public Sub SetVehicleSpecs(ByVal specs As OpenBveApi.Runtime.VehicleSpecs) Implements OpenBveApi.Runtime.IRuntime.SetVehicleSpecs
        Specifications = specs
    End Sub

    Public Sub Unload() Implements OpenBveApi.Runtime.IRuntime.Unload

    End Sub

    Function StartATO()
        If DoorsClosed() = True Then
            If signalaspect.Aspect <> 0 Then
                atoactive = True
            End If
        End If
        Return True
    End Function

    Function DoorsClosed()
        If doorstate = DoorStates.None Then
            Return True
        Else
            Return False
        End If
    End Function
End Class
