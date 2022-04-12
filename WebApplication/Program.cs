// For this application to work you will need a trial license.
// The MyAppKeyPair.snk linked in the project is not present in the repository, 
// you should generate your own strong name key and keep it private.
//
// 1) You can generate a strong name key with the following command in the Visual Studio command prompt:
//     sn -k MyKeyPair.snk
//
// 2) The next step is to extract the public key file from the strong name key (which is a key pair):
//     sn -p MyKeyPair.snk MyPublicKey.snk
//
// 3) Display the public key token for the public key: 	
//     sn -t MyPublicKey.snk
//
// 4) Go to the project properties Signing tab (or Build -> signing in VS2022), 
//    and check the "Sign the assembly" checkbox, and choose the strong name key you created.
//
// 5) Register and get your trial license from https://www.woutware.com/SoftwareLicenses.
//    Enter your strong name key public key token that you got at step 3.
WW.WWLicense.SetLicense("<license string>");

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
