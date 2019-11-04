using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace ASEPerfIssue.Controllers
{
	[Route("api/[controller]")]
	public class HomeController : Controller
	{
		internal IAuthenticationProvider authProvider;
		private ILogger _logger;
		public HomeController(IAuthenticationProvider authentication, ILogger<HomeController> logger)
		{
			authProvider = authentication;
			_logger = logger;
		}

		[HttpGet]
		public async Task<List<string>> Index()
		{
			_logger.LogInformation("Starting Index() method to get all groups.");
			GraphServiceClient graphServiceClient = new GraphServiceClient(authProvider);
			var groups = await graphServiceClient.Groups.Request().GetAsync();
			_logger.LogInformation("End Index() method to get all groups.");
			return groups.Select(s => s.DisplayName).ToList();
		}

		[HttpGet]
		[Route("GetAllOwners")]
		public async Task<List<string>> GetAllOwners()
		{
			_logger.LogInformation("Start GetAllOwners() method to get all group owners.");
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
			_logger.LogInformation("End GetAllOwners() method to get all group owners.");
			return approvers.Distinct().ToList();
		}
	}
}