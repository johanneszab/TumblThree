﻿using System.ComponentModel;

using TumblThree.Domain.Attributes;
using TumblThree.Domain.Converter;
using TumblThree.Domain.Properties;

namespace TumblThree.Domain.Models
{
	[TypeConverter(typeof(EnumDescriptionTypeConverter))]
	public enum MegaTypes
	{
		[LocalizedDescription("mp4", typeof(Resources))]
		Mp4,
		[LocalizedDescription("webm", typeof(Resources))]
		Webm
	}
}