using System.Runtime.Serialization;

namespace InterfaceRpcDemoSharedSG
{
	[DataContract]
	public class Person
	{
		[DataMember(Order = 1)]
		public int Id { get; set; }

		[DataMember(Order = 2)]
		public string FirstName { get; set; }

		[DataMember(Order = 3)]
		public string LastName { get; set; }
	}
}
