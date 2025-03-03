import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

import { Instrument } from '../types/instrument';

@Injectable({
  providedIn: 'root',
})
export class BacktestService {
  constructor(private client: HttpClient) {}

  parse(content: string) {
    const endpointUrl = new URL('/backtest/parse', environment.apiBaseUrl);
    return this.client.post<{}>(endpointUrl.toString(), {
      content: content,
    });
  }

  get_universe() {
    const endpointUrl = new URL('/backtest/universe', environment.apiBaseUrl);
    return this.client.get<Instrument[]>(endpointUrl.toString());
  }
}