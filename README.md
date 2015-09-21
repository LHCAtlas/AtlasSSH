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