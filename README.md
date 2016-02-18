# AtlasSSH
Commands to run Atlas software from a library via SSH to a Linux node

Introduction
============

The goals for version 1.0:

   * Send commands to a Linux node, get back the responses
   * Sufficient functionality to download a dataset from from the GRID
   * Be able to copy that dataset locally to a portable

There are, obviously, stretch goals. For everything, see the issues up on github.

What is in here?
================

I am open to suggestions of other functionality that should be included in this.
The below list are things that are currently in the repo, plans (see the issues section)
are not included here.

   * A Powershell command, Get-GRIDDataset. Given a dataset, it will download it to
     a local server (depending on configuration).
   * A library that can be used in another program (like an analysis) that will effect
     the same result as the Powershell command.

Why Do This?
============

The driving goal is actually data preservation. As input to an analysis I want to specify a GRID
dataset. In order to do that, I need a good way to turn the GRID dataset into a list of files that
can be fed to the ROOT program. In short, a list of UNC paths on windows. This library is meant to do that.

One thing I wasn't planning on at first was the Powershell command. That has turned out to be useful
because I can, with one line (once passwords, etc., are configured) download a dataset. It is much easier
than normal.

How Does It Work?
=================

Because of encryption libraries (I think) no GRID tools work on Windows. This library is thus a horrible
kludge in the sense that it uses ssh into a Linux box and runs the commands and looks at the output
that comes back. The remote box must be setup in certain ways for this to work. And if the tools change
their output format... well, there will have to be patches. :-)

Installation
============

Windows 10:

There is a onetime setup you must do in order to declare the myget feed where these commands are published to
one-get:

  Register-PSRepository -name "atlas-myget" -source https://www.myget.org/F/gwatts-powershell/api/v2 -InstallationPolicy Trusted

That done, you should now be able to locate the module for installation:

   find-module PSAtlasDatasetCommands

And if you are happy with the response, install it:

   find-module PSAtlasDatasetCommands | Install-Module -scope CurrentUser

You can also run that from an admin console, and leave off the Scope. After it is installed, to get the most
recent version, use:

   Update-Module PSAtlasDatasetCommands


Development
===========

Make sure the version numbers in the nuspec and psd1 file's track.
Run nuget pack from the PSAtlasDatasetCommands directory after building in release mode.
Upload to myget (or wherever) for the Powershell commands.

Building the library
====================

Make sure to build in release mode.

Using powershell, from the AtlasWorkflows directory:

    nuget pack -IncludeReferencedProjects -Prop Configuration=Release
	nuget push AtlasSSH.XXX.nupkg

