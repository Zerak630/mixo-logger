import { Injectable } from "@angular/core";
import config from "../../env/env.local.json";
import { ConfigFile } from "../models/config";

@Injectable({
  providedIn: 'root'
})
export default class ConfigService {
  config: ConfigFile = config;
}
