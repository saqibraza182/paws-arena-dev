using EdjCase.ICP.Candid.Mapping;

namespace Candid.World.Models
{
	public class Tokens
	{
		[CandidName("e8s")]
		public ulong E8s { get; set; }

		public Tokens(ulong e8s)
		{
			this.E8s = e8s;
		}

		public Tokens()
		{
		}
	}
}