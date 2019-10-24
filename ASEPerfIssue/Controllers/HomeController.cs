using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;

namespace ASEPerfIssue.Controllers
{
	[Route("api/[controller]")]
	public class HomeController : Controller
	{
		internal IAuthenticationProvider authProvider;
		public HomeController(IAuthenticationProvider authentication)
		{
			authProvider = authentication;
		}

		[HttpGet]
		public async Task<List<string>> Index()
		{
			GraphServiceClient graphServiceClient = new GraphServiceClient(authProvider);
			var groups = await graphServiceClient.Groups.Request().GetAsync();
			return groups.Select(s => s.DisplayName).ToList();
		}

		[HttpGet]
		[Route("GetAllOwners")]
		public async Task<List<string>> GetAllOwners()
		{
			GraphServiceClient graphServiceClient = new GraphServiceClient(authProvider);
			var groups = await graphServiceClient.Groups.Request().GetAsync();
			List<Task<IGroupOwnersCollectionWithReferencesPage>> t = new List<Task<IGroupOwnersCollectionWithReferencesPage>>();
			foreach (Group group in groups)
			{
				t.Add(graphServiceClient.Groups[group.Id].Owners.Request().GetAsync());
			}
			await Task.WhenAll(t);
			List<string> approvers = new List<string>();
			foreach (Task<IGroupOwnersCollectionWithReferencesPage> groupTask in t)
			{
				IGroupOwnersCollectionWithReferencesPage owners = groupTask.Result;
				List<string> approverMail = (from user in owners.Cast<User>().ToList()
											 where user.Mail != null
											 select user.Mail).ToList();
				approvers.AddRange(approverMail);
			}
			return approvers.Distinct().ToList();
		}
	}
}