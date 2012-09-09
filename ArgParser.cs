using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections;
using System.Diagnostics;

namespace PeteRichardson.Utilities {
	
	/// <summary>
	/// Base class for the various kinds of Argument attributes.   Not used directly.
	/// </summary>
	/// <remarks>
	/// Base class for the various kinds of Argument attributes.   Not used directly.
	/// Use one of the derived classes instead.  Derived classes include 
	/// <see cref="OptionAttribute">OptionAttribute</see>, 
	/// <see cref="UsageAttribute">UsageAttribute</see>, and 
	/// <see cref="ParameterListAttribute">ParameterListAttribute</see> instead.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	public abstract class ArgumentAttribute : Attribute
	{
		private int found;
		private bool required;
		private string variableName;
		private string variableType;

		///	<summary>
		///	True if the argument must be supplied on the command line.
		///	</summary>
		/// <remarks>
		/// If an argument is required, then ArgParser.Parse() will throw an
		/// argument exception if it is not specified on the command line.
		/// </remarks>
		/// <include file='ArgParserDocs.xml' path='docs/doc[@for="ArgumentAttribute.Required"]/*' />
		public bool Required
		{
			get {return required;}
			set {required = value;}
		}		
		
		/// <summary>
		/// Returns the number of times the argument was supplied on the command line
		/// </summary>
		/// <remarks>
		///	This is particularly useful after the arguments have been parsed, if you
		/// wish to do further processing (to ensure dependencies between arguments, for
		/// example).
		/// </remarks>
		/// <include file='ArgParserDocs.xml' path='docs/doc[@for="ArgumentAttribute.Found"]/*' />
		public int Found
		{
			get {return found;}
			set {found = value;}
		}
		
		/// <summary>
		/// The name of the variable this attribute applies to.
		/// </summary>
		internal string VariableName
		{
			get {return variableName;}
			set {variableName = value;}
		}

		/// <summary>
		/// The type of the variable this attribute applies to.
		/// </summary>
		internal string VariableType
		{
			get {return variableType;}
			set {variableType = value;}
		}

	}

	
	///	<summary>
	/// Used to indicate the string[] variable where optionless
	/// parameters should be stored.
	/// </summary>
	/// <remarks>
	/// The string[] variable tagged by the ParameterList attribute will be assigned an
	/// array of strings by the <see cref="ArgParser.Parse">Parse()</see> function.  The array will
	/// contain every string on the command line that does not correspond to a particular
	/// option.
	/// </remarks>
	/// <include file='ArgParserDocs.xml' path='docs/doc[@for="ParameterListAttribute"]/*' />
	[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	sealed public class ParameterListAttribute : ArgumentAttribute
	{
		private ArrayList paramList;   

		public ParameterListAttribute()
		{
			paramList = new ArrayList();
		}

		/// <summary>
		/// After all parsing is done, this returns an array of all the optionless parameters
		/// </summary>
		internal string[] Parameters
		{
			get { return (string[]) paramList.ToArray(Type.GetType("System.String")); }
		}

		/// <summary>
		/// Used to add strings to the list of optionless parameters.
		/// This method exists to avoid exposing the underlying implementation
		/// (currently an ArrayList)
		/// </summary>
		/// <param name="s">The optionless parameter to be added to the parameters list</param>
		internal void Add(string s)
		{
			paramList.Add(s);
		}

	}

	
	///	<summary>
	///	Used to indicate the variables that correspond to command line options
	///	</summary>
	///	<remarks>
	///	<P>Place an option attribute before each static or instance variable in your program
	///	that should be filled in by the argument parser.   In each attribute you can
	///	specify the command line argument to be matched against.
	///	</P>
	///	<P>
	///	The variable that follows the OptionAttribute must be type <c>bool</c>,
	///	<c>int</c>, <c>string</c> or <c>string[]</c>.   Currently, no other types
	///	are supported.</P>
	///	<P>In general, the parser looks at the type of the variable, and collects 
	///	enough information from the arg string to initialize it.   Variables can
	///	be separated from values by ':', '=' or ' '.</P>
	///	<P><B><c>bool</c></B> - <c>bool</c> variables are initialized as follows:<br/>
	///	The value for bools can be specified as '+', '-', 'on', 'off', 'yes' or 'no'.
	///	Additionally, the + and - can be tacked on to the option name.  See below
	///	for examples of valid boolean option specifications.
	///	</P>
	///	</remarks>
	/// <include file='ArgParserDocs.xml' path='docs/doc[@for="OptionAttribute"]/*' />
	[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
	public class OptionAttribute : ArgumentAttribute
	{
		protected string text;

		
		/// <summary>
		/// Indicates that the following variable should be set automatically
		/// by the ArgParser.Parse function when one of the strings in the text
		/// parameter is passed on the command line.
		/// </summary>
		/// <param name="text">This should be a comma separated list of strings the user can type on
		/// the command line to trigger the option.</param>
		/// <remarks>
		/// <P>The parser will only match the options with the exact strings
		/// listed in the attribute, so you must specify all shortcuts explicitly.</P>
		/// </remarks>
		/// <include file='ArgParserDocs.xml' path='docs/doc[@for="OptionAttribute Constructor"]/*' />
		public  OptionAttribute(string text)
		{
			this.text = text;
		}

		internal string OptionText
		{
			get {return text;}
			set {text = value;}
		}

	}
	
	
	///	<summary>
	/// Used to indicate the string that contains usage/help information
	/// </summary>
	/// <remarks>
	/// This attribute can only be specified once.
	/// </remarks>
	/// <include file='ArgParserDocs.xml' path='docs/doc[@for="UsageAttribute"]/*' />
	[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	sealed public class UsageAttribute : OptionAttribute
	{
		/// <summary>
		/// Indicates that the following string should be written to the console when
		/// one of the strings in the text parameter is passed on the command line
		/// </summary>
		/// <param name="text">This should be a comma separated list of strings the user can type on
		///	the command line to display help.</param>
		/// <remarks>
		/// The ArgParser will also print the usage string if no arguments are passed to the tool
		/// </remarks>
		/// <include file='ArgParserDocs.xml' path='docs/doc[@for="UsageAttribute Constructor"]/*' />
		public  UsageAttribute(string text) : base(text)
		{
			this.text = text;
		}
	}

	
	/// <summary>
	/// Does the actual work of parsing arguments and setting the variables indicated
	/// by Option and ParameterList attributes.
	/// </summary>
	/// <remarks>
	/// The ArgParser class contains a Parse() function that takes an object or type
	/// and sets static or instance variables that have been tagged with certain attributes
	/// based on the passed in command line.
	/// </remarks>
	/// <include file='ArgParserDocs.xml' path='docs/doc[@for="ArgParser"]/*' />
	public class ArgParser 
	{
		private Type appType;
		private object appInstance;
		private ArrayList argVariables;
		private ParameterListAttribute parametersList;
		private UsageAttribute usage;
		private bool allowSeparateBooleanParameters = false; // don't allow "-bool on" by default; it's ambiguous
		private bool allowSlashOptions = true; // if false, options must start with dash; this allows parameter values that start with slashes.
		private bool addUnknownOptionsToParameterList = false;  // if true, unrecognized options are added to the parameter list instead of 
		
		/// <summary>
		/// Allows you to access the list of argument attributes for the running program.
		/// </summary>
		/// <remarks>
		/// <P>This is useful if you need to access the attribute properties
		/// (like <see cref="ArgumentAttribute.Found">Found</see>)
		/// to do custom argument processing (to ensure dependencies between arguments, for
		/// example).</P>
		/// </remarks>
		/// <include file='ArgParserDocs.xml' path='docs/doc[@for="ArgParser Indexer"]/*' />
		public ArgumentAttribute this [string index] 
		{
			get 
			{
				foreach (OptionAttribute att in argVariables)
					if (ArgMatches(index,att))
						return att;
				return null;
			}
		}

		/// <summary>
		/// Enable/disable boolean arguments with parameters separated by a space; defaults to disabled.
		/// </summary>
		/// <remarks>
		/// Normally, boolean arguments work in two flavors:
		/// <ul><li>"-b", with no parameter: the corresponding boolean variable is "toggled";</li>
		/// <li>"-b=off", with a parameter that specifies the desired state of the corresponding boolean variable.</li></ul>
		/// AllowSeparateBooleanParameters enables a third flavor, where the optional parameter is separated
		/// from the flag argument by a space, "-b off". Because the parameter is optional, this is ambiguous: consider 
		/// a list of filenames where the first happens to be "false" - that's why this behavior is disabled by default.
		/// </remarks>
		public bool AllowSeparateBooleanParameters {
			get 
			{
				return allowSeparateBooleanParameters; 
			}
			set 
			{
				allowSeparateBooleanParameters = value; 
			}
		}

		/// <summary>
		/// Should we treat arguments that start with "/" as options? Defaults to "yes" (true).
		/// </summary>
		/// <remarks>
		/// Normally, options can begin with -dash or /slash; if it's necessary for parameter values to
		/// begin with a /slash (such as partial URLs or Source Depot paths), set this property to false.
		/// </remarks>
		public bool AllowSlashOptions {
			get {
				return allowSlashOptions; 
			}
			set {
				allowSlashOptions = value; 
			}
		}

		/// <summary>
		/// Should we ignore unrecognized options and just add them to the parameter list? Defaults to "no" (false).
		/// By default, unknown options will cause an exception.
		/// </summary>
		/// <remarks>
		/// Sometimes, an option shouldn't start with -dash or /slash;  An example is the 'sync' in 'sd sync' 
		/// </remarks>
		public bool AddUnknownOptionsToParameterList
		{
			get 
			{
				return addUnknownOptionsToParameterList; 
			}
			set 
			{
				addUnknownOptionsToParameterList = value; 
			}
		}

		/// <summary>
		/// Sets variables tagged with attributes to values read from the args[] array.
		/// </summary>
		/// <remarks>
		/// <P>The Parse function reflects on the passed in object or type, looking up all the variables
		/// tagged with Option, Usage or ParameterList attributes, and then parses the args
		/// array and fills in the tagged variables appropriately.</P>
		/// </remarks>
		/// <param name="app">The object or type containing the variables that have been tagged with
		/// attributes.   Usually this is an object representing the application, but it
		/// doesn't have to be.   If all the tagged variables are static, then you should just
		/// pass in the System.Type containing the variables. (See the first example below).
		/// If you are setting at least one instance (non-static) variable, then you must pass
		/// an instance of the type containing the variables.   See the second and third
		/// examples below.</param>
		/// <param name="args">the list of args from the command line.</param>
		/// <include file='ArgParserDocs.xml' path='docs/doc[@for="ArgParser.Parse"]/*' />
		public void Parse(object app, string[] args)
		{
			appInstance = app;
			appType =  app is Type ? (Type) app : appInstance.GetType();

			argVariables = FindArgVariables(appType);
			parametersList = FindParameterList(appType);
			usage = FindUsageString(appType);

			//if ((usage != null) && (args.Length==0))
			//	PrintUsage();

			if (argVariables.Count == 0)
				throw new ArgumentException("No [Option] attributes defined in this app!");

			for (int i = 0; i < args.Length; i++)
			{
				string arg = args[i];

				// (basically, IsOption says "does it start with a '-','/', or '--'")
				if (IsOption(arg))
				{
					// Try to extract the argument name and value from the current token
					// e.g.  if the token is "-name=pete", argNameText='name' and argValue='pete'
					// if the token is "-debug", then argNameText ='debug' and argValue=''
					Regex regex = new Regex(allowSlashOptions ? @"^(?:--?|/)([\?\w]*)[:=]?(.*)" : @"^(?:--?)([\?\w]*)[:=]?(.*)");
					string argNameText = regex.Match(arg).Groups[1].Value;
					string argValue = regex.Match(arg).Groups[2].Value;
					// note: it's OK if argValue is empty.  Depending on the type of the
					// variable we're trying to fill in, we might use the next token and
					// we might not.  That decision is left up to the individual Parse<type> 
					// functions below.

					object newval = null;
					// Now that we have a token from the command line, try to match it against
					// all the various attribute-tagged variables.
					foreach (OptionAttribute argVar in argVariables)
					{
						// ArgMatches just does a simple string compare with each possible alias
						// e.g. if the attribute was [Option("project,p,proj")] then ArgMatches
						// would compare argNameText with "project", then "p", then "proj".
						// If there isn't an exact match, move on.
						if (ArgMatches(argNameText, argVar))
						{
							argVar.Found++;

							// special handler for UsageAttribute.  If we matched it, then PrintUsage()
							if (argVar is UsageAttribute)
							{
								PrintUsage();
								newval = "usage";
								break;
							}

							// Exactly how you parse the next chunk of the command line depends on what
							// variable you're trying to fill.  So switch on the var type.
							// e.g. if the cmd line is "-d -project", and "d" is the OptionText for a 
							// boolean variable, then we don't need to look at the next token (-project)
							// we know we can just toggle the boolean var.
							// But if "d" represents a string variable, then we want to assign "-project" 
							// to that string variable.
							//
							// Each of the Parse&lt;Type&gt; functions returns a value of the appropriate 
							// type based on the remainder of the command line.
							switch(argVar.VariableType)
							{
								case "System.Boolean":
								{
									newval = ParseBoolean(args, ref i, arg, ref argValue, argVar);
									break;
								}
								case "System.Int32":
								{
									newval = ParseInt32(args, ref i, arg, ref argValue);
									break;
								}
								case "System.String":
								{
									newval = ParseString(args, ref i, arg, ref argValue);
									break;
								}
								case "System.String[]":
								{
									newval = ParseStringArray(args, ref i, arg, ref argValue);
									break;
								}
								default:
									throw new ArgumentException("only boolean, int, string and string[] vars are allowed");
							}
							// Here's where we actually set the specified variable to the
							// value figured out by the Parse functions.
							SetField(appInstance, argVar.VariableName, newval );
							break;
						}
					}
					// We've tried to match with every attribute-tagged variable, but couldn't.  D'oh!
					if (newval == null)
					{
						// if we should add unknown options to the parameter list
						if (addUnknownOptionsToParameterList)
							AddToParameterList(args[i]);
						else
							throw new ArgumentException(String.Format("unknown argument: {0}", argNameText));
				
					}
				}
				else
					// Not an option, must be an optionless parameter.   If the user has specified a
					// ParameterList variable, then great.  Add this to the list.  Otherwise throw.
					if (parametersList != null)
					parametersList.Add(args[i]);
				else
					throw new ArgumentException(String.Format("unexpected optionless parameter:  I don't know what to do with \"{0}\"", args[i]));
			}

			// We're done walking the args list.   If the tool writer has specified a ParameterList variable
			// then set it here.   Note that we don't need to throw on the else case, since that was
			// noticed back in the loop when we saw the first optionless parameter.
			if (parametersList != null)
				SetField(appInstance, parametersList.VariableName, parametersList.Parameters);
			
			// If you've asked for help then don't fail if required args aren't found.
			// This keeps "mytool -h" from spitting out error messages if mytool 'requires' other args.
			if ((usage != null) && (args.Length==0 || (this[usage.OptionText].Found > 0)))
				return;

			// throw if any required argument wasn't found
			foreach (OptionAttribute arg in argVariables)
				if (arg.Required && (arg.Found == 0) && !(args.Length==0))
					throw new ArgumentException(String.Format("Missing Required arg: {0}", arg.OptionText));

		}

		// How do we add something to the parameterList
		protected void AddToParameterList(string arg)
		{
			if (parametersList != null)
				parametersList.Add(arg);
			else
				throw new ArgumentException(String.Format("unexpected optionless parameter:  I don't know what to do with \"{0}\"", arg));
		}

		// Does the string match any of the given aliases for the passed in attribute?
		protected bool ArgMatches(string argText, OptionAttribute att)
		{
			// if you passed in an exact match for the option text... Great!  return true
			if (argText == att.OptionText)
				return true;

			// Aliases is the list of individual possible text strings.  Try to match each one exactly.
			string[] aliases = Regex.Split(Regex.Escape(att.OptionText), ",");
			argText = Regex.Escape(argText);
			foreach (string s in aliases)
				if (s == argText)
					return true;
			return false;
		}

		internal bool IsOption(string arg)
		{
			// This first test is here to enable the boolean switch  "-d -" (i.e. turn off the d option)
			if (arg == "-")
				return false;
			return (arg.StartsWith("-") | (allowSlashOptions && arg.StartsWith("/") && ! arg.StartsWith("//")));
		}

		protected bool ParseBoolean(string[] args, ref int i, string arg, ref string argValue, OptionAttribute argVar)
		{
			bool result = false;
			if (arg.EndsWith("+")) argValue = "+";
			if (arg.EndsWith("-")) argValue = "-";

			// if the user wants us to allow possibly-ambiguous separated options for booleans,
			// and there was no value with the boolean option (like -v:on) and if there
			// are more arguments and the next one isn't an option, then try to interpret it
			// as a boolean value.   this allows "-v on".
			if (allowSeparateBooleanParameters && argValue.Length==0 && i<args.Length-1 && !(IsOption(args[i+1])))
				argValue = args[++i];

			switch (argValue.ToLower())
			{
				// this case Toggles the default value of the variable.  It allows variables with
				// both "on by default" and "off by default" senses to work as expected.
				// e.g. if you declare bool debug = false;, then -d should turn ON debugging.
				// but if you declare bool verbose = true;, then -q should turn OFF verbose output.
				case "":
					result = ! (bool) GetField(appInstance, argVar.VariableName);
					break;
				case "on": case "true": case "yes": case "+":
					result = true; break;
				case "off": case "false": case "no": case "-":
					result = false; break;
				default:
					throw new ArgumentException(String.Format("{0} requires a boolean parameter (like true or false, off or on, yes or no, + or -).", arg));
			}
			return result;
		}

		protected int ParseInt32(string[] args, ref int i, string arg, ref string argValue)
		{
			if (argValue.Length == 0)
				if (i<args.Length-1)
					argValue = args[++i];
				else
					throw new ArgumentException(String.Format("{0} requires an integer parameter.", arg));
			return Convert.ToInt32(argValue);
		}

		//TODO:  Implement ParseInt32Array()

		protected string ParseString(string[] args, ref int i, string arg, ref string argValue)
		{
			if (argValue.Length == 0)
				if (i<args.Length-1)
					argValue = args[++i];
				else
					throw new ArgumentException(String.Format("{0} requires a string parameter.", arg));
			return argValue;
		}

		protected string[] ParseStringArray(string[] args, ref int i, string arg, ref string argValue)
		{
			ArrayList list = new ArrayList();
			if (argValue.Length == 0)
				if (i<args.Length-1)
				{
					int j=i+1;
					if (args[j].IndexOf(",") > 0)
					{
						list.AddRange(Regex.Split(args[j], ","));
						i++;
					}
					else
						while ((j < args.Length) && !Regex.IsMatch(args[j], allowSlashOptions ? "^(--?|/)" : "^(--?)"))
						{
							list.Add(args[j++]);
							i++;
						}
					argValue = String.Join(" ", (string[]) list.ToArray(Type.GetType("System.String")) );
				}
				else
				{
					throw new ArgumentException(String.Format("{0} requires one or more strings as parameters.", arg));
				}
			else
				if (argValue.IndexOf(",") > 0)
				list.AddRange(Regex.Split(argValue, ","));
			else
				list.Add(argValue);
								
			return (string[]) list.ToArray(Type.GetType("System.String"));
		}
		
		// Reflect over the type looking for instance variables that are tagged with Option attributes.
		// This code would work with static variables too, but I can't figure out how to "set" them. (see 
		// SetField() below.
		internal ArrayList FindArgVariables(Type t) 
		{
         
			OptionAttribute att;
			ArrayList result = new ArrayList();

			//Get all fields in this class and put them in an array of System.Reflection.MemberInfo objects.
			FieldInfo[] MyFieldInfo = t.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

			//Loop through all fields in this class looking for ArgumentAttributes
			for(int i = 0; i < MyFieldInfo.Length; i++)
			{         
				att = (OptionAttribute) Attribute.GetCustomAttribute(MyFieldInfo[i], typeof (OptionAttribute));
				if(null != att)
				{
					att.VariableName = MyFieldInfo[i].Name;
					att.VariableType = MyFieldInfo[i].FieldType.ToString();
					result.Add(att);
				}      
			}

			//foreach (OptionAttribute arg in result)
				//Console.WriteLine("\t\t{0}\t{1}\t{2}\t{3}", arg.OptionText, arg.Required, arg.VariableName, arg.VariableType);
			
			return result;
		}

		// Walk the parameters again looking for the ParameterList.    Could probably merge this
		// into FindArgVariables()...  they're very similar.
		internal ParameterListAttribute FindParameterList(Type t) 
		{
			ParameterListAttribute result = null;
			FieldInfo[] MyFieldInfo = t.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			for(int i = 0; i < MyFieldInfo.Length; i++)
			{         
				result = (ParameterListAttribute) Attribute.GetCustomAttribute(MyFieldInfo[i], typeof (ParameterListAttribute));
				if(result != null)
				{
					result.VariableName = MyFieldInfo[i].Name;
					result.VariableType = MyFieldInfo[i].FieldType.ToString();
					break;
				}
			}
			// walked entire list, but no Parameters attribute declared.  Return null.
			return result;
		}
	
		// Walk the parameters again looking for the Usage string.    Could probably merge this
		// into FindArgVariables()...  they're very similar.
		internal UsageAttribute FindUsageString(Type t) 
		{
			UsageAttribute result = null;
			FieldInfo[] MyFieldInfo = t.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			for(int i = 0; i < MyFieldInfo.Length; i++)
			{         
				result = (UsageAttribute) Attribute.GetCustomAttribute(MyFieldInfo[i], typeof (UsageAttribute));
				if(null != result)
				{
					result.VariableName = MyFieldInfo[i].Name;
					result.VariableType = MyFieldInfo[i].FieldType.ToString();
					Debug.Assert(result.VariableType == "System.String", 
							@"Usage Attribute must be applied to a variable of type System.String, not " + result.VariableType);
						
					break;
				}
			}
			// walked entire list, but no Usage attribute declared
			return result;
		}
	
		internal void PrintUsage()
		{
			Console.WriteLine( (string) GetField(appInstance, usage.VariableName));
		}

		// Set this field in this object to this value
		internal void SetField(object o, string fieldName, object value)
		{
			FieldInfo myFieldInfo = appType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static); 
			myFieldInfo.SetValue(o, value, BindingFlags.Public| BindingFlags.NonPublic, null, null);
		}
		internal object GetField(object o, string fieldName)
		{
			FieldInfo myFieldInfo = appType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static); 
			return myFieldInfo.GetValue(o);
		}

        public static void Parse(string[] args) {
            ArgParser ap = new ArgParser();
            try {
                Assembly exasm = Assembly.GetEntryAssembly();
                ap.Parse(exasm.EntryPoint.DeclaringType, args);
                if (ap["h,help,?"].Found > 0) 
                    Environment.Exit(0);
            } catch (ArgumentException e) {
                Console.WriteLine(e.Message);
                Environment.Exit(1);
            }
        }

	}
}
