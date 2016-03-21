
# About

Clockz is a utility library that provides a set of clock and clock tick implementations. Some implementations are more targeted for testing and other for production usage. All current implemenations are created due to specific problem at the time and I thought that this code could be useful to others.

It has two abtractions, `IClock` and `ITicks` but both are different enough to make a distinction.

The `IClock` abstraction is about time. Most developers see time as something precise but the thruth is time is everything but precise. A computure clock is inaccurate and constantly corrected. This is caused by leap years, daylight saving time, clock drift for remote time server (NTP) corrections, power saving features and virtualization. So a clock can be used to check, how many hours, days, months or years have passed where in most circumstances (unless you work at NASA) you usually do not care about the fractions or accuracy.

On the other hand there is the need for measuring things like how long did to send an email, process 1,000 items and that is where `ITicks` is meant for. Ticks are used to measure time between two occurences. Yes, you could use clocks for that in certain conditions but as said, clocks are constantly adjusted. So if something took 1.5 days, was that with or without clock drift and / or daylight saving time correction?

Very often these are combined. Like, at March 21th at 13:37 the system sent an email and took Y milliseconds. Noticed thatI didn't mention the seconds and milliseconds in the timestamp?

# Example

One reason could be to sync your clock to a remote clock to keep all timestamps in sync. Usually you do this by syncing your clock with an NTP source like for example a domain controller but often domain controllers itself are not correctly synced with 'internet time'.

All clocks implement the IClock interface.


# Current clock implementations

Most clocks can be usefull in actual applications but last clocks are ment for testing components that need the current time either for business logic.

## SystemClock

Guess what this implementation does.

## AdjustedClock

Takes a clock provider and corrects it with a fixed offset.

## CompensatedClock

Takes a clock provider and corrects it with a dynamic offset based on passed time.

## SntpClock

Retrieves the time from a NTP source. Care should be take with this clock as continiously requesting the time could result your IP to be blocked. It is recommended to use this clock in combination with the AdjustedClock or SyncClock.

## SqlClock

Retrieves the time from a SQL database server.

## StopwatchClock

Takes a source clock for the first 'pulse' and then uses the Stopwatch to measure passed time and calculate the current time.

## SyncClock

This clock takes a two clocks and a expiration offset. The source and other which could be systemclock and ntp clock. The idea is that this implementation calculates the difference between the two clocks. It corrects the 'source' clock with difference of the 'other' clock. This is needed in environments that often need a timestamp but cannot continuously request the time of a remote source like for example an remote NTP server.

## FixedClock

This clock is initialized with a fixed value. This is only usefull to unit test certain components that for example should perform an action when a certain amount of time has passed.

## QueuedClock

This clock is initialized with a couple of DateTime values and can only be usefull in unittests to test for age cases.

# Current ticks implementations

When choosing a ticks implementation you basically decide if you need high accuracy and / or low latency.

## StopwatchTicks

The `StopwatchTicks` uses the `Stopwatch` from the diagnostics namespace. It is the most accurate timing source and especially useful for measuring tiny tasks.

## ClockTicks

The clock implementation is there more for environments to make it explicit that time is measured even when using clocks. Its not very suitable to measure tiny tasks due to the latency.

## EnvironmentTicks

This relies on the environment ticks. It has the drawback that is uses a `int` and this value becomes negative in the .net framework API. The Clockz implementation cast it to a `uint` to fix this rollover issue as the `ITicks` interface uses a `long`.

If it is running on a system that supports `GetTickCount64` then it will use that to retrieve the environment ticks.

# Usage

You can use each clock by creating the specific clock but its better to just use a single point in your application to store a clock instance.

## Global static readonly

In asp.net this could be Global.asax by defining and then injecting this global clock when needed.

    public static readonly IClock Clock = SystemClock.Instance;


## IOC

Define your clock in your IOC container of choice. For example Castle Windsor.

    IWindsorContainer Container = new WindsorContainer();
    Container.Register(Component.For<IClock>().Instance());

A second option is to register a Func<DateTime> instead of the IClock interface. That would make your components not rely on the Clockz assembly.

    IWindsorContainer Container = new WindsorContainer();
    Func<DateTime> dateFunc = ()=>SystemClock.Instance.UtcNow;
    Container.Register(Component.For<Func<DateTime>>().Instance(dateFunc));


# Build

There are no other dependacies to be loaded but currently only .net 4 is targeted although my guess it that it will work from 2.0 and up and maybe even 1.0.


# License

This is provided under Creative Commons Attribution-ShareAlike 3.0 (CC-BY-SA).

For more information about this license go to : http://creativecommons.org/licenses/by-sa/3.0/

There are also licenses provided for the dependencies:

## nunit

Testing framework
<http://www.nunit.org/index.php?p=license&r=2.6>

## SNTP Client

SNTP client by Valer BOCAN vbocan@dataman.ro
There are no restrictions on how you use the code provided herein, except a short notice to the author.
<http://www.dataman.ro/?page_id=39>
