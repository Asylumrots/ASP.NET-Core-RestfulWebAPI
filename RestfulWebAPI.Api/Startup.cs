using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Marvin.Cache.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using RestfulWebAPI.Api.Data;
using RestfulWebAPI.Api.Services;

namespace RestfulWebAPI.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //开启http缓存headers 需安装Marvin.cache.headers Nuget包   自定义配置
            services.AddHttpCacheHeaders(expires =>
            {
                expires.MaxAge = 60; //缓存时间
                expires.CacheLocation = CacheLocation.Private;  //缓存状态是private还是public
            }, validation =>
            {
                validation.MustRevalidate = true;//如果响应已经过期必须重新进行验证 true
            });
            //开启响应缓存机制（不开启在控制器上写标签无效）
            services.AddResponseCaching();
            //内容协商 Accpet application/json application/xml
            services.AddControllers(setup =>
            {
                setup.ReturnHttpNotAcceptable = true;//允许http当接收不正确数据格式的时候返回406 默认为false这处更改为ture
                //setup.OutputFormatters.Add(new XmlDataConstractSerializerOutputFormatters); //旧格式
                setup.CacheProfiles.Add("120sCacheProfile",new CacheProfile()
                {
                    Duration = 120
                });//自定义缓存的名称和设置，以便于复用
            }).AddNewtonsoftJson(setup =>
                {//使用NewtonsoftJson来响应httppatch请求的数据 提前装好AspNetCore.MVC.NewtonsoftJson
                    setup.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                }).AddXmlDataContractSerializerFormatters()   //添加xml格式 默认只有json
                .ConfigureApiBehaviorOptions(setup =>
                {//自定义错误信息
                    setup.InvalidModelStateResponseFactory = context =>
                    {
                        var problemDetails = new ValidationProblemDetails(context.ModelState)
                        {
                            Type = "http://www.baidu.com",
                            Title = "发生了一个错误",
                            Status = StatusCodes.Status422UnprocessableEntity,
                            Detail = "请查看详细信息",
                            Instance = context.HttpContext.Request.Path
                        };

                        problemDetails.Extensions.Add("traceId", context.HttpContext.TraceIdentifier);

                        return new UnprocessableEntityObjectResult(problemDetails)
                        {
                            ContentTypes = { "application/problem+json" }
                        };
                    };
                });

            services.Configure<MvcOptions>(config =>
            {
                //取出newtonJsonDotNet输出格式化器
                var newtonSoftJsonOutputFormatter                                   //如果不为空则返回第一个
                    = config.OutputFormatters.OfType<NewtonsoftJsonOutputFormatter>()?.FirstOrDefault();

                //如果不为空则  全局添加自定义的mediaType类型支持
                newtonSoftJsonOutputFormatter?.SupportedMediaTypes.Add("application/vnd.company.hateoas+json");

            });

            //注册 Mapper服务（模型映射） 提前安装好MapperNuget包
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            //注册服务
            services.AddScoped<ICompanyRepository, CompanyRepository>();

            //注册DbContext服务
            services.AddDbContext<MyDbContext>(option =>
            {
                option.UseSqlite("Data Source=routine.db");
            });

            //注册自己的映射服务
            services.AddTransient<IPropertyMappingService, PropertyMappingService>();
            services.AddTransient<IPropertyCheckerService, PropertyCheckerService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else //当抛出未处理的异常时用户的页面
            {
                app.UseExceptionHandler(appBuilder =>
                {
                    appBuilder.Run(async context =>
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("Unexpected Error!");
                    });
                });
            }
            //开启缓存中间件
            //app.UseResponseCaching(); //微软自带

            //必须在此处  ResponseCaching之后 其余之前
            app.UseHttpCacheHeaders();

            app.UseStaticFiles();//允许静态文件

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
