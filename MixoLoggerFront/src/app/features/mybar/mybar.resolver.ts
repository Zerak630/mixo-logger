import { inject } from "@angular/core";
import { ResolveFn } from "@angular/router";
import { MyBar } from "../../models/bar";
import { MyBarService } from "./mybar.service";

export const myBarResolver: ResolveFn<MyBar> = (route, state) => {
	return inject(MyBarService).getMyBar();
};
