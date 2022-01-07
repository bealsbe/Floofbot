// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "<Pending>")]              // We have some readonly modifiers that exist on stuff that just can't be readonly (such as databases)
[assembly: SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "<Pending>")]               // Pattern matching can look pretty messy at times, especially in a lot of the cases we use
[assembly: SuppressMessage("Style", "IDE0071:Interpolation can be simplified", Justification = "<Pending>")]    // Interpolation can be simplified at the cost of readability
[assembly: SuppressMessage("Style", "IDE0057:Use range operator", Justification = "<Pending>")]                 // Range operator in a lot of the given cases doesn't exactly make sense (0-EOF??)
[assembly: SuppressMessage("Style", "IDE0018:Inline variable declaration", Justification = "<Pending>")]        // Inline variable declarations cause slight
                                                                                                                //  readability issues, where you can't tell where the variable is defined
//[assembly: SuppressMessage("Style", "IDE0075:Simplify conditional expression", Justification = "<Pending>")]    // A large number of these in the future will be requesting us to use &&


// This target is specifically defined as we don't always want to suppress these and currently only have the single instance
//  compared to say, IDE0044:Add readonly modifier, where there are many instances.
[assembly: SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>", Scope = "member", Target = "~M:Floofbot.Utilities.ServerList~System.Threading.Tasks.Task")]

// These values are read, I think? I'm not sure, but for now we're keeping these here. They're likely used in reflection or a static
//  type somewhere, I dunno.
[assembly: SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "<Pending>", Scope = "member", Target = "~F:Floofbot.Services.WelcomeGateService._floofDb")]
[assembly: SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "<Pending>", Scope = "member", Target = "~F:Floofbot.Program._handler")]
[assembly: SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "<Pending>", Scope = "member", Target = "~F:Floofbot.Modules.Help._serviceProvider")]

// This using statement is written specifically for readability. Leaving this here.
[assembly: SuppressMessage("Style", "IDE0063:Use simple 'using' statement", Justification = "<Pending>", Scope = "member", Target = "~M:Floofbot.Modules.Helpers.ApiFetcher.RequestStringFromApi(System.String,System.String)~System.Threading.Tasks.Task{System.String}")]
