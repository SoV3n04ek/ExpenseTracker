using ExpenseTracker.API.Middleware;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Application.Services;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using ExpenseTracker.Application.Validators;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, b => 
        b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
    ));

builder.Services.AddScoped<IExpenseService, ExpenseService>();

builder.Services.AddValidatorsFromAssemblyContaining<CreateExpenseDtoValidator>();
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

app.UseCustomExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.UseHttpsRedirection();

app.Run();