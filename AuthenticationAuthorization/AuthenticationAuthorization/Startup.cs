using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace AuthenticationAuthorization
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseAuthentication(); // Authentication

            app.UseEndpoints(endpoints =>
            {
                #region Menu
                endpoints.MapGet("/", async context =>
                {
                    string content = "<h1>ASP.NET Core 인증과 권한 초간단 코드</h1>";

                    content += "<a href=\"/Login\">로그인</a><br />";
                    content += "<a href=\"/Login/User\">로그인(User)</a><br />";
                    content += "<a href=\"/Login/Administrator\">로그인(Administrator)</a><br />";
                    content += "<a href=\"/Info\">정보</a><br />";
                    content += "<a href=\"/InfoDetails\">정보(Details)</a><br />";
                    content += "<a href=\"/InfoJson\">정보(JSON)</a><br />";
                    content += "<a href=\"/Logout\">로그아웃</a><br />";

                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8"; // ko-KR
                    await context.Response.WriteAsync(content);
                });
                #endregion

                #region Login
                endpoints.MapGet("/Login", async context =>
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, "User Name")
                    };

                    var claimIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var claimPrincipal = new ClaimsPrincipal(claimIdentity);

                    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimPrincipal);

                    string content = "<h3>로그인 완료</h3>";
                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8"; // ko-KR
                    await context.Response.WriteAsync(content);
                });
                #endregion

                #region Login/{Username}
                endpoints.MapGet("/Login/{Username}", async context =>
                {
                    var username = context.Request.RouteValues["Username"].ToString();

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, username),
                        new Claim(ClaimTypes.Name, username),
                        new Claim(ClaimTypes.Email, username + @"@a.com"),
                        new Claim(ClaimTypes.Role, "Users"),
                        new Claim("원하는 이름", "원하는 값"),
                    };

                    if (username == "Administrator")
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "Administrators"));
                    }

                    var claimIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var claimPrincipal = new ClaimsPrincipal(claimIdentity);

                    await context.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        claimPrincipal,
                        new AuthenticationProperties { IsPersistent = true }); // 쿠키 영구 저장

                    string content = "<h3>로그인 완료</h3>";
                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8"; // ko-KR
                    await context.Response.WriteAsync(content);
                });
                #endregion

                #region Info
                endpoints.MapGet("/Info", async context =>
                {
                    string result = "";

                    if (context.User.Identity.IsAuthenticated)
                    {
                        result += $"<h3>로그인 이름 : {context.User.Identity.Name}</h3>";
                    }
                    else
                    {
                        result += "<h3>로그인하지 않았습니다.</h3>";
                    }

                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8"; // ko-KR
                                                                                           //await context.Response.WriteAsync(result, Encoding.Default);
                    await context.Response.WriteAsync(result);
                });
                #endregion

                #region InfoDetails
                endpoints.MapGet("/InfoDetails", async context =>
                {
                    string result = "";

                    if (context.User.Identity.IsAuthenticated)
                    {
                        result += $"<h3>로그인 이름 : {context.User.Identity.Name}</h3>";
                        foreach (var claim in context.User.Claims)
                        {
                            result += $"{claim.Type} = {claim.Value}<br />";
                        }

                        if (context.User.IsInRole("Administrators") && context.User.IsInRole("Users"))
                        {
                            result += $"<br />Administrators + Users 권한이 있습니다.<br />";
                        }
                    }
                    else
                    {
                        result += "<h3>로그인하지 않았습니다.</h3>";
                    }

                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8"; // ko-KR
                                                                                           //await context.Response.WriteAsync(result, Encoding.Default);
                    await context.Response.WriteAsync(result);
                });
                #endregion

                #region InfoJson
                endpoints.MapGet("/InfoJson", async context =>
                {
                    string json = "";

                    if (context.User.Identity.IsAuthenticated)
                    {
                        var claims = context.User.Claims.Select(o => new ClaimDto { Type = o.Type, Value = o.Value });
                        var options = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                        json += JsonSerializer.Serialize<IEnumerable<ClaimDto>>(claims, options);
                        //json += $"<h3>로그인 이름 : {context.User.Identity.Name}</h3>";
                    }
                    else
                    {
                        json += "{}";
                    }

                    // MIME 타입
                    //context.Response.Headers["Content-Type"] = "text/html; charset=utf-8"; // ko-KR
                    context.Response.Headers["Content-Type"] = "application/json; charset=utf-8"; // ko-KR
                    //await context.Response.WriteAsync(result, Encoding.Default);
                    await context.Response.WriteAsync(json);
                });
                #endregion

                #region Logout
                endpoints.MapGet("/Logout", async context =>
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                    string content = "<h3>로그아웃 완료</h3>";
                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8"; // ko-KR
                    await context.Response.WriteAsync(content);
                });
                #endregion
            });
        }
    }

    /// <summary>
    /// Data Transfer Object
    /// </summary>
    public class ClaimDto
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }
}
