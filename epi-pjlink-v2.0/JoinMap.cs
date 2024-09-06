using PepperDash.Essentials.Core;

namespace PJLinkProjectorEpi
{
    public class JoinMap : JoinMapBaseAdvanced
    {
        [JoinName("Power Off")]
        public JoinDataComplete PowerOff = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital,
                Description = "Power Off"
            });

        [JoinName("Power On")]
        public JoinDataComplete PowerOn = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital,
                Description = "Power On"
            });

        [JoinName("Warming")]
        public JoinDataComplete Warming = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 11,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital,
                Description = "Warming"
            });

        [JoinName("Cooling")]
        public JoinDataComplete Cooling = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 12,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
			      JoinType = eJoinType.Digital,
                Description = "Cooling"
            });

        [JoinName("Mute Off")]
        public JoinDataComplete MuteOff = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 21,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital,
                Description = "Mute Off"
            });

        [JoinName("Mute On")]
        public JoinDataComplete MuteOn = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 22,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital,
                Description = "Mute On"
            });

        [JoinName("Mute Toggle")]
        public JoinDataComplete MuteToggle = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 23,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital,
                Description = "Mute Toggle"
            });

        [JoinName("Is Projector")]
        public JoinDataComplete IsProjector = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 3,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital,
                Description = "Is Projector"
            });

        [JoinName("Is Online")]
        public JoinDataComplete IsOnline = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 50,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital,
                Description = "Is Online"
            });

        [JoinName("Name")]
        public JoinDataComplete Name = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial,
                Description = "Name"
            });

        [JoinName("Status")]
        public JoinDataComplete Status = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial,
                Description = "Status"
            });

        [JoinName("Serial Number")]
        public JoinDataComplete SerialNumber = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 3,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial,
                Description = "Serial Number"
            });

        [JoinName("Lamp Hours")]
        public JoinDataComplete LampHours = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog,
                Description = "Lamp Hours"
            });
		[JoinName("Lens Position")]
		public JoinDataComplete LensPositionMemory = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 5,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Analog,
				Description = "Lens Position"
			});
        [JoinName("Input Select Offset")]
        public JoinDataComplete InputSelectOffset = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 11,
                JoinSpan = 10
            },
            new JoinMetadata()
            {
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.DigitalAnalog,
                Description = "Input Select"
            });
        [JoinName("Freeze Off")]
        public JoinDataComplete FreezeOff = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 29,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital,
                Description = "Freeze Off"
            });

        [JoinName("Freeze On")]
        public JoinDataComplete FreezeOn = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 30,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital,
                Description = "Freeze On"
            });

        [JoinName("Freeze Toggle")]
        public JoinDataComplete FreezeToggle = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 37,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital,
                Description = "Freeze Toggle"
            });
        [JoinName("VShiftPlus")]
		public JoinDataComplete VShiftPlus = new JoinDataComplete(new JoinData { JoinNumber = 40, JoinSpan = 1 },
			new JoinMetadata { Description = "VShiftPlus", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("VShiftMinus")]
		public JoinDataComplete VShiftMinus = new JoinDataComplete(new JoinData { JoinNumber = 41, JoinSpan = 1 },
			new JoinMetadata { Description = "VShiftMinus", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("HShiftPlus")]
		public JoinDataComplete HShiftPlus = new JoinDataComplete(new JoinData { JoinNumber = 42, JoinSpan = 1 },
			new JoinMetadata { Description = "HShiftPlus", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("HShiftMinus")]
		public JoinDataComplete HShiftMinus = new JoinDataComplete(new JoinData { JoinNumber = 43, JoinSpan = 1 },
			new JoinMetadata { Description = "HShiftMinus", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("FocusPlus")]
		public JoinDataComplete FocusPlus = new JoinDataComplete(new JoinData { JoinNumber = 44, JoinSpan = 1 },
			new JoinMetadata { Description = "FocusPlus", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("FocusMinus")]
		public JoinDataComplete FocusMinus = new JoinDataComplete(new JoinData { JoinNumber = 45, JoinSpan = 1 },
			new JoinMetadata { Description = "FocusMinus", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("ZoomPlus")]
		public JoinDataComplete ZoomPlus = new JoinDataComplete(new JoinData { JoinNumber = 46, JoinSpan = 1 },
			new JoinMetadata { Description = "ZoomPlus", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("ZoomMinus")]
		public JoinDataComplete ZoomMinus = new JoinDataComplete(new JoinData { JoinNumber = 47, JoinSpan = 1 },
			new JoinMetadata { Description = "ZoomMinus", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });


        public JoinMap(uint joinStart)
            : base(joinStart, typeof(JoinMap))
        {

        }
    }
}