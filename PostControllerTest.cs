using AutoMapper;
using deha_api_exam.Controllers;
using deha_api_exam.Models;
using deha_api_exam.Services;
using deha_api_exam.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Text;

namespace deha_api_test
{
    public class JwtOptions
    {
        public string? Issuer { get; set; }
        public string? Audience { get; set; }
        public string? SigningKey { get; set; }

        public JwtOptions()
        {
            Issuer = "https://api.vnLab.com";
            Audience = "https://api.vnLab.com";
            SigningKey = "this is my custom Secret key for authentication";
        }
    }
    public class PostControllerTest
    {
        private Mock<IPostService> _postService;
        private PostController _postController;
        private Mock<IMapper> _mapper;
        private Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private JwtOptions _jwtOptions;
        public PostControllerTest()
        {
            _jwtOptions = new JwtOptions();
            _postService = new Mock<IPostService>();
            _mapper = new Mock<IMapper>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _postController = new PostController(_postService.Object, _mapper.Object);
            _postController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            //     _factory = factory;
        }


        public IEnumerable<PostViewModel> getTestModel()
        {
            var myListModel = new List<PostViewModel> {
                new PostViewModel(1, "Post số 1 của tôi", "Bài post này có nội dung", "example_userID"),
                new PostViewModel(2, "Post số 2 của tôi", "Bài post này có nội dung 2", "example_userID"),
            };
            return myListModel;
        }

        [Fact]
        public async Task GetAll()
        {
            // Arrange
            var myListModel = getTestModel();
            // Act
            _postService.Setup(repo => repo.GetAll()).ReturnsAsync(myListModel);

            var result = await _postController.GetAll();

            var okResult = result as OkObjectResult;
            //   var response = await _httpClient.GetAsync("/Home");

            // Assert
            //   Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(myListModel, okResult.Value);
        }

        [Fact]
        public async Task GetAllByUser()
        {
            // Arrange
            var myListModel = getTestModel();
            
            _postService.Setup(repo => repo.GetAllByUser(It.IsAny<string>())).ReturnsAsync(myListModel);
            // Act
            var result = await _postController.GetAllByUser("example_userID");

            var okResult = result as OkObjectResult;
            //   var response = await _httpClient.GetAsync("/Home");

            // Assert
            //   Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(myListModel, okResult.Value);
        }

        [Fact]
        public async Task GetAllPagingAndFilter()
        {
            // Arrange
            var myListModel = getTestModel();
            
            _postService.Setup(repo => repo.GetAllPagingAndFilter(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(myListModel);
            // Act
            var result = await _postController.GetAllPagingAndFilter("Post",1);

            var okResult = result as OkObjectResult;
            //   var response = await _httpClient.GetAsync("/Home");

            // Assert
            //   Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(myListModel, okResult.Value);
        }

        [Fact]
        public async void GetByIDSuccess()
        {
            // Arrange
            var TestModel = new PostViewModel(1, "Post số 1 của tôi", "Bài post này có nội dung", "example_userID");
            _postService.Setup(repo => repo.GetById(It.IsAny<int>())).ReturnsAsync(TestModel);
            //Act
            var returnvalue = await _postController.GetById(1);
            var result = returnvalue  as OkObjectResult;
            //Assert
            Assert.NotNull(result);
            Assert.Equal(TestModel, result.Value);
        }

        [Fact]
        public async void GetByIDNotFound()
        {
            // Arrange
         //   var TestModel = new PostViewModel(1, "Post số 1 của tôi", "Bài post này có nội dung", "example_userID");
            _postService.Setup(repo => repo.GetById(It.IsAny<int>())).ReturnsAsync((int id) => null);
            //Act
            var returnvalue = await _postController.GetById(1);
            
            var result = returnvalue  as NotFoundResult;
            //Assert
            Assert.NotNull(result);
            Assert.Equal(404,result.StatusCode);
        }


        [Fact]
        public async void GetPostWithCommentSuccess()
        {
            // Arrange
            var TestModel = new PostwithComment(1, "Post số 1 của tôi", "Bài post này có nội dung", DateTime.Now,0,0,"example_userID",null,"example_name");
            _postService.Setup(repo => repo.GetPostwithComment(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(TestModel);
            //Act
            var returnvalue = await _postController.GetPostWithComment(1,1);
            var result = returnvalue as OkObjectResult;
            //Assert
            Assert.NotNull(result);
            Assert.Equal(TestModel, result.Value);
        }

        [Fact]
        public async void GetPostWithCommentNotFound()
        {
            // Arrange
            //   var TestModel = new PostViewModel(1, "Post số 1 của tôi", "Bài post này có nội dung", "example_userID");
            _postService.Setup(repo => repo.GetPostwithComment(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((int PostID,int page) => null);
            //Act
            var returnvalue = await _postController.GetPostWithComment(1,1);

            var result = returnvalue as NotFoundResult;
            //Assert
            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async void CreateSuccess()
        {
            // Arrange
            var TestModel = new PostRequest("This is my title","this is my content");
            var userid = "example_userID";
            SetupToken(userid);
            _postService.Setup(repo => repo.Create(It.IsAny<PostRequest>(), It.IsAny<string>())).ReturnsAsync(new Result { type = "Success", message = "Add post success!" });
            //Act
            var returnvalue = await _postController.Create(TestModel);
            var result = returnvalue as CreatedAtActionResult;
            //Assert
            Assert.NotNull(result);
            Assert.Equal(TestModel, result.Value);
        }

        [Fact]
        public async void CreateFailureUserNotFound()
        {
            // Arrange
            var TestModel = new PostRequest("This is my title", "this is my content");
            var userid = "example_userID";
            SetupToken(userid);
            _postService.Setup(repo => repo.Create(It.IsAny<PostRequest>(), It.IsAny<string>())).ReturnsAsync(new Result { type = "NotFound", message = "User not found so can't create post" });
            //Act
            var returnvalue = await _postController.Create(TestModel);
            var result = returnvalue as NotFoundObjectResult;
            //Assert
            Assert.NotNull(result);
            Assert.Equal("User not found so can't create post", result.Value);
        }

        [Fact]
        public async void CreateFailureModelisnotValid()
        {
            // Arrange
            var TestModel = new PostRequest("mytitle", "this is my content");
            var userid = "example_userID";
            SetupToken(userid);
            _postService.Setup(repo => repo.Create(It.IsAny<PostRequest>(), It.IsAny<string>())).ReturnsAsync(new Result { type = "Failure", message = "Model isn't valid" });
            //Act
            var returnvalue = await _postController.Create(TestModel);
            var result = returnvalue as BadRequestObjectResult;
            //Assert
            Assert.NotNull(result);
            Assert.Equal("Model isn't valid", result.Value);
        }


        [Fact]
        public async void UpdateSuccess()
        {
            // Arrange
            var TestModel = new PostViewModel(1, "Post số 1 của tôi", "Bài post này có nội dung", "example_userID");
            var UpdateModel = new PostUpdateRequest(1, "Post so 1 update", "Content moi");
            //  _postService.Setup(repo => repo.Update(It.IsAny<PostViewModel>())).ReturnsAsync(TestModel);
            var userid = "example_userID";
            var UpdateModelMapper = new PostViewModel(1, "Post so 1 update", "Content moi", "example_userID");
            SetupToken(userid);
            _postService.Setup(repo => repo.GetById(It.IsAny<int>())).ReturnsAsync(TestModel);
            _postService.Setup(service => service.Update(UpdateModelMapper)).ReturnsAsync(new Result { type = "Success", message = "Success" });
            _mapper.Setup(map => map.Map<PostViewModel>(UpdateModel)).Returns(UpdateModelMapper);
            //Act
            var returnvalue = await _postController.Update(UpdateModel);

            var result = returnvalue as OkObjectResult;
            //Assert
            Assert.NotNull(result);
            Assert.Equal("Success", result.Value);
        }


        [Fact]
        public async void UpdateModelisnotValid()
        {
            // Arrange
            var TestModel = new PostViewModel(1, "Post số 1 của tôi", "Bài post này có nội dung", "example_userID");
            var UpdateModel = new PostUpdateRequest(1, "update", "Content moi");
            var UpdateModelMapper = new PostViewModel(1, "update", "Content moi", "example_userID");
            //  _postService.Setup(repo => repo.Update(It.IsAny<PostViewModel>())).ReturnsAsync(TestModel);
            var userid = "example_userID";
            SetupToken(userid);
            _postService.Setup(repo => repo.GetById(It.IsAny<int>())).ReturnsAsync(TestModel);
            _postService.Setup(service => service.Update(UpdateModelMapper)).ReturnsAsync(new Result { type = "Failure", message = "Model isn't valid" });
            _mapper.Setup(map => map.Map<PostViewModel>(UpdateModel)).Returns(UpdateModelMapper);
            //Act
            var returnvalue = await _postController.Update(UpdateModel);

            var result = returnvalue as OkObjectResult;
            //Assert
            Assert.NotNull(result);
            Assert.Equal("Model isn't valid", result.Value);
        }


        [Fact]
        public async void UpdateNotFound()
        {
            // Arrange
            var TestModel = new PostViewModel(1, "Post số 1 của tôi", "Bài post này có nội dung", "example_userID");
            var UpdateModel = new PostUpdateRequest(1, "Post so 1 update", "Content moi");
            _postService.Setup(repo => repo.Update(It.IsAny<PostViewModel>())).ReturnsAsync(new Result { type = "NotFound", message = "NotFound" });
            var userid = "example_userID";
            SetupToken(userid);
            //Act
            var returnvalue = await _postController.Update(UpdateModel);
            var result = returnvalue as NotFoundObjectResult;
            //Assert
            Assert.NotNull(result);
            Assert.Equal("Post not found", result.Value);
        }

        [Fact]
        public async void UpdateBadRequestNotCreator()
        {
            // Arrange
            var TestModel = new PostViewModel(1, "Post số 1 của tôi", "Bài post này có nội dung", "example_userID");
            var UpdateModel = new PostUpdateRequest(1, "Post so 1 update", "Content moi");
          //  _postService.Setup(repo => repo.Update(It.IsAny<PostViewModel>())).ReturnsAsync(TestModel);
            var userid = "example_userID2";
            SetupToken(userid);
            _postService.Setup(repo => repo.GetById(It.IsAny<int>())).ReturnsAsync(TestModel);
            _postService.Setup(service => service.Update(TestModel)).ReturnsAsync(new Result { type = "Success", message = "Success" });
            //Act
            var returnvalue = await _postController.Update(UpdateModel);

            var result = returnvalue as BadRequestObjectResult;
            //Assert
            Assert.NotNull(result);
            Assert.Equal("Can't edit this post cause you aren't post's creator", result.Value);
        }



        public void SetupToken(string UserID)
        {
            //for token
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, UserID),
                // Add other claims if necessary
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey!));
            var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                _jwtOptions.Issuer,
                _jwtOptions.Audience,
                claims,
                expires: DateTime.Now.AddHours(3),
                signingCredentials: signingCredentials);

            var handler = new JwtSecurityTokenHandler();
            var tokenString = handler.WriteToken(token);
            //set up header
            _postController.ControllerContext.HttpContext.Request.Headers["Authorization"] = $"Bearer {tokenString}";
            _mockHttpContextAccessor.Setup(_ => _.HttpContext).Returns(_postController.ControllerContext.HttpContext);
        }
        [Fact]
        public async void DeletebyIDSuccess()
        {
            // Arrange
            var TestModel = new PostViewModel(1, "Post số 1 của tôi", "Bài post này có nội dung", "example_userID");
            _postService.Setup(repo => repo.GetById(It.IsAny<int>())).ReturnsAsync(TestModel);
            var userid = "example_userID";
            SetupToken(userid);
            _postService.Setup(service => service.Delete(1)).ReturnsAsync(new Result { type = "Success", message = "Deleted successfully" });
            //Act
            var returnvalue = await _postController.Delete(1);
            var result = returnvalue as NoContentResult;
            //Assert
            Assert.NotNull(result);
            Assert.Equal(204, result.StatusCode);
        }

        [Fact]
        public async void DeletebyIDNotFound()
        {
            // Arrange
            var TestModel = new PostViewModel(1, "Post số 1 của tôi", "Bài post này có nội dung", "example_userID");
            _postService.Setup(repo => repo.GetById(It.IsAny<int>())).ReturnsAsync((int id) => null);
            var userid = "example_userID";
            SetupToken(userid);
       //     _postService.Setup(service => service.Delete(1)).ReturnsAsync(new Result { type = "Success", message = "Deleted successfully" });
            //Act
            var returnvalue = await _postController.Delete(4);
            var result = returnvalue as NotFoundObjectResult;
            //Assert
            Assert.NotNull(result);
            Assert.Equal("Post not found", result.Value);
        }


        [Fact]
        public async void DeletebyIDBadRequestNotCreator()
        {
            // Arrange
            var TestModel = new PostViewModel(1, "Post số 1 của tôi", "Bài post này có nội dung", "example_userID");
            _postService.Setup(repo => repo.GetById(It.IsAny<int>())).ReturnsAsync(TestModel);
            var userid = "example_userID2";
            #region comment
            //for token
            /*     var claims = new List<Claim>
                 {
                     new Claim(ClaimTypes.NameIdentifier, userid),
                     // Add other claims if necessary
                 };

                 var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey!));
                 var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                 var token = new JwtSecurityToken(
                     _jwtOptions.Issuer,
                     _jwtOptions.Audience,
                     claims,
                     expires: DateTime.Now.AddHours(3),
                     signingCredentials: signingCredentials);

                 var handler = new JwtSecurityTokenHandler();
                 var tokenString = handler.WriteToken(token);   */
            //    var headers = new HeaderDictionary();
            //    headers.Add("Authorization", $"Bearer {tokenString}");
            //    var httpContext = new DefaultHttpContext();
            //    httpContext.Request.Headers.Add("Authorization", $"Bearer {tokenString}");
            //     _mockHttpContextAccessor.Setup(_ => _.HttpContext).Returns(httpContext);

            //       _postController.ControllerContext.HttpContext.Request.Headers["Authorization"] = $"Bearer {tokenString}";
            //       _mockHttpContextAccessor.Setup(_ => _.HttpContext).Returns(_postController.ControllerContext.HttpContext);
            #endregion
            SetupToken(userid);
            _postService.Setup(service => service.Delete(1)).ReturnsAsync(new Result { type="Success", message = "Deleted successfully" });
            //Act
            var returnvalue = await _postController.Delete(1);

            var result = returnvalue as BadRequestObjectResult;
            //Assert
            Assert.NotNull(result);
            Assert.Equal("Can't delete this post cause you aren't post's creator", result.Value);
        }

        [Fact]
        public async void PatchVoteNotFound()
        {
            // Arrange
            //  var TestModel = new PostViewModel(1, "Post số 1 của tôi", "Bài post này có nội dung", "example_userID");
            var id = 1;
            var userid = "example_userID";
         //   var patchDocument = new JsonPatchDocument<VoteViewModel>();
         //   patchDocument.Replace(x => x.UserID, userid);
         //   patchDocument.Replace(x => x.PostID, id);
            _postService.Setup(repo => repo.PatchVote(It.IsAny<int>(), It.IsAny<JsonPatchDocument<VoteViewModel>>())).ReturnsAsync(new Result { type = "NotFound", message = "NotFound" });
            SetupToken(userid);
            //     _postService.Setup(service => service.Delete(1)).ReturnsAsync(new Result { type = "Success", message = "Deleted successfully" });
            //Act
            var returnvalue = await _postController.PatchVote(1);
            var result = returnvalue as NotFoundObjectResult;
            //Assert
            Assert.NotNull(result);
            Assert.Equal("Post not found", result.Value);
        }

        [Fact]
        public async void PatchVoteAlreadyVote()
        {
            // Arrange
            //  var TestModel = new PostViewModel(1, "Post số 1 của tôi", "Bài post này có nội dung", "example_userID");
            var id = 1;
            var userid = "example_userID";
            //   var patchDocument = new JsonPatchDocument<VoteViewModel>();
            //   patchDocument.Replace(x => x.UserID, userid);
            //   patchDocument.Replace(x => x.PostID, id);
            _postService.Setup(repo => repo.PatchVote(It.IsAny<int>(), It.IsAny<JsonPatchDocument<VoteViewModel>>())).ReturnsAsync(new Result { type = "Failure", message = "This user already vote this post" });
            SetupToken(userid);
            //     _postService.Setup(service => service.Delete(1)).ReturnsAsync(new Result { type = "Success", message = "Deleted successfully" });
            //Act
            var returnvalue = await _postController.PatchVote(1);
            var result = returnvalue as BadRequestObjectResult;
            //Assert
            Assert.NotNull(result);
            Assert.Equal("This user already vote this post", result.Value);
        }

        [Fact]
        public async void PatchVoteSuccess()
        {
            // Arrange
            //  var TestModel = new PostViewModel(1, "Post số 1 của tôi", "Bài post này có nội dung", "example_userID");
            var id = 1;
            var userid = "example_userID";
            _postService.Setup(repo => repo.PatchVote(It.IsAny<int>(), It.IsAny<JsonPatchDocument<VoteViewModel>>())).ReturnsAsync(new Result { type = "Success", message = "Vote Success" });
            SetupToken(userid);
            //     _postService.Setup(service => service.Delete(1)).ReturnsAsync(new Result { type = "Success", message = "Deleted successfully" });
            //Act
            var returnvalue = await _postController.PatchVote(1);
            var result = returnvalue as OkObjectResult;
            //Assert
            Assert.NotNull(result);
            Assert.Equal("Success", result.Value);
        }

        //ViewCount

        [Fact]
        public async void PatchViewCountNotFound()
        {
            // Arrange
            //  var TestModel = new PostViewModel(1, "Post số 1 của tôi", "Bài post này có nội dung", "example_userID");
            var id = 1;
            var userid = "example_userID";
            var patchDocument = new JsonPatchDocument<ViewCountPatch>();
            patchDocument.Replace(x => x.ViewCount, 1);
            _postService.Setup(repo => repo.PatchViewCount(It.IsAny<int>(), It.IsAny<JsonPatchDocument<ViewCountPatch>>())).ReturnsAsync(new Result { type = "NotFound", message = "NotFound" });
            SetupToken(userid);
            //     _postService.Setup(service => service.Delete(1)).ReturnsAsync(new Result { type = "Success", message = "Deleted successfully" });
            //Act
            var returnvalue = await _postController.PatchViewCount(1, patchDocument);
            var result = returnvalue as NotFoundResult;
            //Assert
            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async void PatchViewCountBadRequest()
        {
            // Arrange
            //  var TestModel = new PostViewModel(1, "Post số 1 của tôi", "Bài post này có nội dung", "example_userID");
            var id = 1;
            var userid = "example_userID";
            var patchDocument = new JsonPatchDocument<ViewCountPatch>();
            patchDocument.Replace(x => x.ViewCount, 1);
            _postService.Setup(repo => repo.PatchViewCount(It.IsAny<int>(), It.IsAny<JsonPatchDocument<ViewCountPatch>>())).ReturnsAsync(new Result { type = "Failure", message = "BadRequest" });
            SetupToken(userid);
            //     _postService.Setup(service => service.Delete(1)).ReturnsAsync(new Result { type = "Success", message = "Deleted successfully" });
            //Act
            var returnvalue = await _postController.PatchViewCount(1,patchDocument);
            var result = returnvalue as BadRequestObjectResult;
            //Assert
            Assert.NotNull(result);
            Assert.Equal("BadRequest", result.Value);
        }

        [Fact]
        public async void PatchViewCountSuccess()
        {
            // Arrange
            //  var TestModel = new PostViewModel(1, "Post số 1 của tôi", "Bài post này có nội dung", "example_userID");
            var id = 1;
            var userid = "example_userID";
            var patchDocument = new JsonPatchDocument<ViewCountPatch>();
            patchDocument.Replace(x => x.ViewCount, 1);
            _postService.Setup(repo => repo.PatchViewCount(It.IsAny<int>(), It.IsAny<JsonPatchDocument<ViewCountPatch>>())).ReturnsAsync(new Result { type = "Success", message = "Vote Success" });
            SetupToken(userid);
            //     _postService.Setup(service => service.Delete(1)).ReturnsAsync(new Result { type = "Success", message = "Deleted successfully" });
            //Act
            var returnvalue = await _postController.PatchViewCount(1,patchDocument);
            var result = returnvalue as OkObjectResult;
            //Assert
            Assert.NotNull(result);
            Assert.Equal("Success", result.Value);
        }








    }
}