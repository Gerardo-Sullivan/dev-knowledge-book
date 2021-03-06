﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using IBM.WatsonDeveloperCloud.NaturalLanguageUnderstanding.v1;
using Google.Cloud.Firestore;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Domain.Services;
using System.Text.Json.Serialization;
using WebApi.Models.Configuration;
using WebApi.ActionFilters;
using WebApi.Models;

namespace WebApi
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
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = DevKnowledgeBookModelStateValidator.ValidateModelState;
            });
            services.Configure<DevKnowledgeBookConfiguration>(Configuration.GetSection("DevKnowledgeBook"));
            services.Configure<IbmConfiguration>(Configuration.GetSection("IBM"));

            services.AddRouting(options =>
            {
                options.LowercaseUrls = true;
            });

            services.AddControllers(options =>
            {
                options.EnableEndpointRouting = false;
                options.Filters.Add<ApiKeyAuthorizationFilter>();
                options.Filters.Add<ApiExceptionFilter>();
            })
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = DevKnowledgeBookModelStateValidator.ValidateModelState;
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            //TODO: Read about singleton vs transient vs scoped https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-3.0
            services.AddSingleton<FirestoreDb>(serviceProvider =>
            {
                return FirestoreDb.Create(Configuration["Firebase:ProjectID"]); //TODO: Add google environment variable https://cloud.google.com/docs/authentication/getting-started
            });

            services.AddSingleton<INaturalLanguageUnderstandingService>(serviceProvider =>
            {
                var IbmConfiguration = serviceProvider.GetService<IOptions<IbmConfiguration>>().Value;
                var naturalLanguageService = new NaturalLanguageUnderstandingService
                {
                    UserName = "ApiKey",
                    Password = IbmConfiguration.ApiKey, //TODO: change for production
                    ApiKey = IbmConfiguration.ApiKey, //TODO: change for production
                    VersionDate = IbmConfiguration.Version,
                };

                naturalLanguageService.SetEndpoint(IbmConfiguration.Url);

                return naturalLanguageService;
            });

            services.AddSingleton<IFirestoreDbContext, FirestoreDbContext>();
            services.AddSingleton<INaturalLanguageService, NaturalLanguageService>();

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "DevKnowledgeBook API", Version = "v1" });
                options.AddSecurityDefinition("Api Key", ApiKeySecurityScheme.Instance());
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { ApiKeySecurityScheme.Instance(), new List<string>() },
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStaticFiles();
            app.UseHttpsRedirection();
            app.UseMvc();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "DevKnowledgeBook API V1");
                options.RoutePrefix = "api/docs";
            });
        }
    }
}
