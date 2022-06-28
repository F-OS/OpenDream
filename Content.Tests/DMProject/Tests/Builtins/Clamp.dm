﻿/proc/RunTest()
	var/out1 = clamp(10, 1, 5)
	ASSERT(out1 == 5)
	var/out2 = clamp(-10, 1, 5)
	ASSERT(out2 == 1)
	var/out3 = clamp(list(-10, 5, 40, -40), 1, 10)
	for(var/item in out3)
		ASSERT(item >= 1 && item <= 10)